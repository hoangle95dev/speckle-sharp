#include "SchemaDefinitionProvider.hpp"


namespace Json {


GS::UniString SchemaDefintionProvider::ElementIdsSchema ()
{
	return R"(
		"ElementIds": {
			"type": "array",
			"description": "Container for element Ids.",
	  		"items": { 
				"type": "string",
				"description": "A Globally Unique Identifier (or Universally Unique Identifier) in its string representation as defined in RFC 4122.",
				"format": "uuid",
				"pattern": "^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$"
			}
		}
	)";
}


}