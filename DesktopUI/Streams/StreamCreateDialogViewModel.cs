﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.DesktopUI.Accounts;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamCreateDialogViewModel : Conductor<IScreen>.Collection.OneActive,
    IHandle<RetrievedFilteredObjectsEvent>, IHandle<UpdateSelectionEvent>, IHandle<ApplicationEvent>
  {
    private readonly IEventAggregator _events;
    private readonly ConnectorBindings _bindings;
    private ISnackbarMessageQueue _notifications = new SnackbarMessageQueue(TimeSpan.FromSeconds(5));

    public StreamCreateDialogViewModel(
      IEventAggregator events,
      StreamsRepository streamsRepo,
      AccountsRepository acctsRepo,
      ConnectorBindings bindings)
    {
      DisplayName = "Create Stream";
      _events = events;
      _bindings = bindings;
      _filterTabs = new BindableCollection<FilterTab>(_bindings.GetSelectionFilters().Select(f => new FilterTab(f)));
      _streamsRepo = streamsRepo;
      _acctRepo = acctsRepo;

      _selectionCount = _bindings.GetSelectedObjects().Count;
      _events.Subscribe(this);
    }

    private readonly StreamsRepository _streamsRepo;
    private readonly AccountsRepository _acctRepo;

    public ISnackbarMessageQueue Notifications
    {
      get => _notifications;
      set => SetAndNotify(ref _notifications, value);
    }

    public string ActiveViewName
    {
      get => _bindings.GetActiveViewName();
    }

    public List<string> ActiveViewObjects
    {
      get => _bindings.GetObjectsInView();
    }

    public List<string> CurrentSelection
    {
      get => _bindings.GetSelectedObjects();
    }

    public List<string> StreamIds;

    private int _selectionCount;

    public int SelectionCount
    {
      get => _selectionCount;
      set => SetAndNotify(ref _selectionCount, value);
    }

    private bool _createButtonLoading;

    public bool CreateButtonLoading
    {
      get => _createButtonLoading;
      set => SetAndNotify(ref _createButtonLoading, value);
    }

    private bool _addExistingButtonLoading;

    public bool AddExistingButtonLoading
    {
      get => _addExistingButtonLoading;
      set => SetAndNotify(ref _addExistingButtonLoading, value);
    }

    private Stream _streamToCreate = new Stream();

    public Stream StreamToCreate
    {
      get => _streamToCreate;
      set => SetAndNotify(ref _streamToCreate, value);
    }

    private StreamState _streamState = new StreamState();

    public StreamState StreamState
    {
      get => _streamState;
      set => SetAndNotify(ref _streamState, value);
    }

    private Account _accountToSendFrom = AccountManager.GetDefaultAccount();

    public Account AccountToSendFrom
    {
      get => _accountToSendFrom;
      set => SetAndNotify(ref _accountToSendFrom, value);
    }

    private BindableCollection<FilterTab> _filterTabs;

    public BindableCollection<FilterTab> FilterTabs
    {
      get => _filterTabs;
      set => SetAndNotify(ref _filterTabs, value);
    }

    private FilterTab _selectedFilterTab;

    public FilterTab SelectedFilterTab
    {
      get => _selectedFilterTab;
      set { SetAndNotify(ref _selectedFilterTab, value); }
    }

    public ObservableCollection<Account> Accounts
    {
      get => _acctRepo.LoadAccounts();
    }

    private int _selectedSlide = 0;

    public int SelectedSlide
    {
      get => _selectedSlide;
      set => SetAndNotify(ref _selectedSlide, value);
    }

    #region Adding Collaborators

    private string _userQuery;

    public string UserQuery
    {
      get => _userQuery;
      set
      {
        SetAndNotify(ref _userQuery, value);
        if ( value == "" )
        {
          SelectedUser = null;
          UserSearchResults.Clear();
        }

        if ( SelectedUser == null )
          SearchForUsers();
      }
    }

    private BindableCollection<User> _userSearchResults;

    public BindableCollection<User> UserSearchResults
    {
      get => _userSearchResults;
      set => SetAndNotify(ref _userSearchResults, value);
    }

    private User _selectedUser;

    public User SelectedUser
    {
      get => _selectedUser;
      set
      {
        SetAndNotify(ref _selectedUser, value);
        if ( SelectedUser == null )
          return;
        UserQuery = SelectedUser.name;
        AddCollabToCollection(SelectedUser);
      }
    }

    private BindableCollection<User> _collaborators = new BindableCollection<User>();

    public BindableCollection<User> Collaborators
    {
      get => _collaborators;
      set => SetAndNotify(ref _collaborators, value);
    }

    #endregion

    #region Searching Existing Streams

    private string _streamQuery;

    public string StreamQuery
    {
      get => _streamQuery;
      set
      {
        SetAndNotify(ref _streamQuery, value);

        if ( value == "" )
        {
          SelectedStream = null;
          StreamSearchResults.Clear();
        }

        if ( SelectedStream == null || value != SelectedStream.name )
          SearchForStreams();
      }
    }

    private BindableCollection<Stream> _streamSearchResults;

    public BindableCollection<Stream> StreamSearchResults
    {
      get => _streamSearchResults;
      set => SetAndNotify(ref _streamSearchResults, value);
    }

    private Stream _selectedStream;

    public Stream SelectedStream
    {
      get => _selectedStream;
      set
      {
        SetAndNotify(ref _selectedStream, value);
        NotifyOfPropertyChange(nameof(CanAddExistingStream));
        if ( SelectedStream == null )
          return;
        StreamQuery = SelectedStream.name;
      }
    }

    private async void SearchForStreams()
    {
      if ( StreamQuery == null || StreamQuery.Length <= 2 )
        return;

      try
      {
        var client = new Client(AccountToSendFrom);
        var streams = await client.StreamSearch(StreamQuery);
        StreamSearchResults = new BindableCollection<Stream>(streams);
        await Task.Delay(300);
      }
      catch ( Exception e )
      {
        Debug.WriteLine(e);
      }
    }

    #endregion

    public void ContinueStreamCreate(string slideIndex)
    {
      if ( StreamQuery == null || StreamQuery.Length < 2 )
      {
        Notifications.Enqueue("Please choose a name for your stream!");
        return;
      }

      AccountToSendFrom = _acctRepo.GetDefault();
      StreamToCreate.name = StreamQuery;
      NotifyOfPropertyChange(nameof(StreamToCreate.name));

      SelectedStream = null;
      ChangeSlide(slideIndex);
    }

    public async void AddNewStream()
    {
      CreateButtonLoading = true;
      var client = new Client(AccountToSendFrom);
      try
      {
        var streamId = await _streamsRepo.CreateStream(StreamToCreate, AccountToSendFrom);

        foreach ( var user in Collaborators )
        {
          var res = await client.StreamGrantPermission(new StreamGrantPermissionInput()
          {
            streamId = streamId, userId = user.id, role = "stream:contributor"
          });
        }

        var filter = SelectedFilterTab.Filter;
        if ( filter.Name == "View" || filter.Name == "Category" )
          filter.Selection = SelectedFilterTab.ListItems.ToList();

        StreamToCreate = await _streamsRepo.GetStream(streamId, AccountToSendFrom);
        StreamState = new StreamState(client, StreamToCreate) {Filter = filter};
        _bindings.AddNewStream(StreamState);

        _events.Publish(new StreamAddedEvent() {NewStream = StreamState});
        StreamState = new StreamState();
        CloseDialog();
      }
      catch ( Exception e )
      {
        await client.StreamDelete(StreamToCreate.id);
        Notifications.Enqueue($"Error: {e.Message}");
      }

      CreateButtonLoading = false;
    }

    public bool CanAddExistingStream => SelectedStream != null;

    public async void AddExistingStream()
    {
      if ( StreamIds.Contains(SelectedStream.id) )
      {
        Notifications.Enqueue("This stream already exists in this file");
        return;
      }

      AddExistingButtonLoading = true;

      var client = new Client(AccountToSendFrom);
      StreamToCreate = await client.StreamGet(SelectedStream.id);

      StreamState = new StreamState(client, StreamToCreate) {ServerUpdates = true};
      _bindings.AddNewStream(StreamState);
      _events.Publish(new StreamAddedEvent() {NewStream = StreamState});

      AddExistingButtonLoading = false;
      CloseDialog();
    }

    public async void SearchForUsers()
    {
      if ( UserQuery == null || UserQuery.Length <= 2 )
        return;

      try
      {
        var client = new Client(AccountToSendFrom);
        var users = await client.UserSearch(UserQuery);
        UserSearchResults = new BindableCollection<User>(users);
        await Task.Delay(300);
      }
      catch ( Exception e )
      {
        Debug.WriteLine(e);
      }
    }

    public void AddSimpleStream()
    {
      CreateButtonLoading = true;
      SelectedFilterTab = FilterTabs.First(tab => tab.Filter.Name == "Selection");
      SelectedFilterTab.Filter.Selection = _bindings.GetSelectedObjects();
      AccountToSendFrom = _acctRepo.GetDefault();
      StreamToCreate.name = StreamQuery;
      SelectedStream = null;

      AddNewStream();
    }

    public void AddStreamFromView()
    {
      SelectedFilterTab = FilterTabs.First(tab => tab.Filter.Name == "Selection");
      SelectedFilterTab.Filter.Selection = ActiveViewObjects;

      AddNewStream();
    }

    // TODO extract dialog logic into separate manager to better handle open / close
    public void CloseDialog()
    {
      DialogHost.CloseDialogCommand.Execute(null, null);
    }

    public void ChangeSlide(string slideIndex)
    {
      SelectedSlide = int.Parse(slideIndex);
    }

    private void AddCollabToCollection(User user)
    {
      if ( Collaborators.All(c => c.id != user.id) )
        Collaborators.Add(user);
    }

    public void RemoveCollabFromCollection(User user)
    {
      Collaborators.Remove(user);
    }

    public void RemoveCatFilter(string name)
    {
      var catFilter = FilterTabs.First(tab => tab.Filter.Name == "Category");
      catFilter.RemoveListItem(name);
    }

    public void RemoveViewFilter(string name)
    {
      var viewFilter = FilterTabs.First(tab => tab.Filter.Name == "View");
      viewFilter.RemoveListItem(name);
    }

    public void Handle(RetrievedFilteredObjectsEvent message)
    {
      StreamState.Placeholders = message.Objects.ToList();
    }

    public void Handle(UpdateSelectionEvent message)
    {
      var selectionFilter = FilterTabs.First(tab => tab.Filter.Name == "Selection");
      selectionFilter.Filter.Selection = message.ObjectIds;

      SelectionCount = message.ObjectIds.Count;
    }

    public void Handle(ApplicationEvent message)
    {
      switch ( message.Type )
      {
        case ApplicationEvent.EventType.ViewActivated:
        {
          NotifyOfPropertyChange(nameof(ActiveViewName));
          NotifyOfPropertyChange(nameof(ActiveViewObjects));
          return;
        }
        case ApplicationEvent.EventType.DocumentClosed:
        {
          CloseDialog();
          return;
        }
        default:
          return;
      }
    }
  }
}
