#Emby Voice commands

Emby voice commands use json and regular expression to find corresponding commands.

With this solution the translation to other languages will be simplified and is fully compatible with regular expression for many programming languages.

###Json template :
```json
[
	{
	"group": "general",
	"name": "General commands",
    "defaultValues": {
            "sourceid": "",
            "deviceid": "",
            "itemName": "",
            "itemType": "",
            "shuffle": false,
            "filters": [],
            "sortBy": "",
            "sortOrder": "",
            "limit": 100,
            "category": ""
        },
	"items": [
			{
				"actionid": "show",
				"sourceid": "movies",
                "menuid" : "home",
                "groupid" : "movie",
                "deviceid": "displaymirroring",
                "command": "(?<action>play|Listen to)\\s?(?<determiner1>my|me)?\\s?(?<source> music)\\s?(?<ArtistName>.*)?\\s?(?<deviceaction>on device|to device)\\s?(?<Devicename>.*)",
                "altcommand": "(?<action>play|Listen to)\\s?(?<determiner1>my|me)?\\s?(?<source> music)\\s?(?<ArtistName>.*)?",
				"itemName": "",
				"itemType": "movie",
                "shuffle": false,
				"filters": [ ],
				"sortBy": "",
				"sortOrder": "Ascending",
                "limit": 100,
                "category": "",
				"commandtemplates": [
					"Show Movie based commands",
					"Show Music based commands",
					"Show Picture based commands",
					"Show TV series based commands",
					"Show general commands"
				]
			}
		]
	}
]
```

###Json hierarchy

>+ Group
    - defaultValues
    - Items
        + commandtemplates

###Json Description :

**Group**
>**groupid** : (mandatory) id of the group commands
>**name** : (mandatory) name of the group

**Items and defaultValues (mandatory)** 

**actionid** : (mandatory) Liste of actions available
>- show 
- play  
- shuffle  
- search 
- control 
- enable 
- disable 
- toggle 

**sourceid** : (optional) source commands available
>- music 
- movies  
- tvseries  
- livetv 
- recordings 
- latestepisodes 
- home 
- group 

**menuid** : (optional) menu commands available
>- Commands for live TV 
 - livetv  
 - guide  
 - channels 
 - recordings 
 - scheduled 
 - series 
 - group 
>- Commands for home menus 
 - home  
 - nextup  
 - favorites 
 - upcoming 
 - nowplaying 

**groupid** : (optional) name of the group   
You can define a group name specified in the json file

**deviceid** : (optional) devices commands available
>- displaymirroring 


**Emby filters** : (optional) 
>- itemName 
- itemType  
- shuffle : default value = false 
- filters 
- sortBy 
- sortOrder 
- limit : default value = 100
- category 

**commandtemplates** (mandatory)   
array : list of text commands


**command** : (mandatory)    
regular expression used to filter commands   
**Exemple :**    
command: "(?<action>play|Listen to)\\s?(?<determiner1>my|me)?\\s?(?<source> music)\\s?(?<ArtistName>.*)?\\s?(?<deviceaction>on device|to device)\\s?(?<Devicename>.*)",
altcommand: "(?<action>play|Listen to)\\s?(?<determiner1>my|me)?\\s?(?<source> music)\\s?(?<ArtistName>.*)?",

####Regular expression description
```json
(?<action>  or ?<MovieName>, etc) - are for defining watts is captured  
my|me - indicate each of those words/phrases can be used  
\\s? - is for spaces  
(?<MovieName>.*)? - the ? at the end of the closing brackets represent an optional value

```

In the example below theses phrases can the match for an action
- Show my movies  
- Show movies  
- Display movies  
- Go to movies  
- etc.  

**altcommand** : (optional)    
alternate regular expression used to filter commands if the property **command** does not match


####Regular expression description
```json
?<action>  or ?<MovieName>, etc - are for defining watts is captured  
my|me - indicate each of those words/phrases can be used  
\s? - is for spaces  
(?<MovieName>.*)? - the ? at the end of the closing brackets represent an optional value

```

####Additional properties used by regular expression   
>**action** : Linked to actionid
>**source** : Linked to sourceid
>**menu** : Linked to menuid
>**group** : Linked to groupid
>**device** : Linked to deviceid
>**determiner1 or determiner2 etc** : used just to capture words
>**moviename** 
>**devicename** 
>**songname** 
>**artistname** 
>**albumname** 
>**seriename** 
>**seasonname** 
>**picturename** 
>**authorname** 