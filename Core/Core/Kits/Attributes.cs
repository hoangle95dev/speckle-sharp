﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Core.Kits
{

  [AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
  public class SchemaInfo : Attribute
  {
    private string _description;
    private string _name;
    public string Subcategory { get; }
    public string Category { get; }
    
    public virtual string Description
    {
      get { return _description; }
    }

    public virtual string Name
    {
      get { return _name; }
    }

    public SchemaInfo(string name, string description, string category = null, string subcategory = null)
    {
      _name = name;
      _description = description;
      Category = category;
      Subcategory = subcategory;
    }
  }

  [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
  public class SchemaParamInfo : Attribute
  {
    private string _description;

    public virtual string Description
    {
      get { return _description; }
    }

    public SchemaParamInfo(string description)
    {
      _description = description;
    }
  }

  /// <summary>
  /// Used to indicate which is the main input parameter of the schema builder component. Schema info will be attached to this object.
  /// </summary>
  [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
  public class SchemaMainParam : Attribute
  {
    public SchemaMainParam()
    {
    }
  }

  // TODO: this could be nuked, as it's only used to hide props on Base, 
  // which we might want to expose anyways...
  /// <summary>
  /// Used to ignore properties from expand objects etc
  /// </summary>
  [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
  public class SchemaIgnore : Attribute
  {
  }
}
