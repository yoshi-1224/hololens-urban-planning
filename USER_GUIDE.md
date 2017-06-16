# User Guide

### On start:
1. When the application starts, room scanning will be activated for 10-15 seconds until the room model can be observed so that the floors
and walls can be replicated inside the application. User should look around the room to help Hololens scan.

2. Once the scanning is complete, user should hear a "ping" sound which indicates that a map object has been created. This map will then follow user's gaze.
While the map is following the user's gaze, it is <b>"placeable"</b>.

  * When the shadow casted by the map object is green, it means that the map is directly above a real-world flat horizontal surface that has been previously scanned. User can tap on the map to place it.
  * When the shadow casted by the map object is red, it means that the map is not directly above a flat horizontal surface that acommodicates its size

Should the user wish to change the position of the map later, either tap on the map object or use the <b>"move map"</b> voice command.
### Once the map is placed:
1. User will see a set of building placed on the map. These building objects represent actual buildings around the Tanjong Pagar MRT area, including Capital Tower and Asia Square.
2. Gaze at one of the building objects. If the object is highlighted upon a gaze focus, this object is <b>interactible</b>

## Interactibles
### Supported voice commands
Voice command | Use
------------ | -------------
move | manipulate the position of this building object with hand gesture. <br>Note that it cannot be moved along the y-axis (i.e. up and down) relative to the map
rotate |  rotate the building object with hand gesture.
show info | shows a table that lists some information on the building. If the table already exists, it will be re-positioned and oriented towards the user.
hide info | hides the table if the table exists in the scene.

### Supported gestures
1. Tap: has the same effect as "show info" voice command. shows a table listing some infomation on the building. If the table already exists, it will be re-positioned and oriented towards the user.

#### Tables
- Tap: dismisses the table. same effect as "hide info" voice command.
- Drag and Drop: moves the table around

## Tool menu
To activate a tool menu, use <b>"show tools"</b> voice command. To hide the tool menu, either click on the close button or use <b>"hide tools"</b> voice command.
- tool menu is draggable. Simply choose an area other than the buttons, tap and hold in order to position it.
### Using the tool menu
- From the tool menu, user is able to choose from three different types of prefabs to place onto the map.
- Tap on one of the buttons to instantiate a prefab. The prefab will show up just below the buttons.

#### Prefabs
- Once instantiated, user can drag and drop them onto the map. If they are successfully placed on the map, they will turn into an <b>"interactible"</b> that behaves the same way as all the other existing building objects (i.e. the same voice commands/gestures are applicable to these).
- However, unlike the existing buildings, user may also delete these instantiated prefabs. To do this, simply gaze at the object and use "delete" voice command.

## Map Scaling
- use <b>"scale map"</b> voice command. User can then scale the map using vertical drap and drop gesture. While scaling, the current scale of the map relative to its real-world size is shown.

* Note: The buildings will also be scaled together with the map so that their relative scale remains constant. Also, the building prefabs will take into account the current scaling of the map as well, and will enlarge/shrink upon drap and drop gesture mentioned above.

## Street view
### Enter street view mode
1. Gaze at a desired point on the map to be located in street view mode. To make sure the map object is being gazed, user may check whether it is highlighted or not.
2. Use <b>"street view"</b> voice command.

#### Supported gestures in street view
- Drag and Drop: move forward/backward, sideways

### Exit street view mode
- Use <b>"exit"</b> voice command
- User will then be able to place the map as he/she did at the start of the application

# Summary of voice commands

Command | Use
------------ | -------------
Move Map | allows user to specify the position on which the map is placed
Scale Map | scales the map with hand gesture together with the buildings
Show Tools | shows the tool menu
Hide Tools | hides the tool menu
Move | allows the user to move the position of the building
Rotate | allows the user to rotate the building
Show info | shows a table with information about the building
Hide info | hides the table
Street View | enters into the street view, with the user initially placed at the gazed position on the map
Exit | exits the street view
Delete | deletes the currently-gazed building, if this building was instantiated by the user

Written by:
Yoshiaki Nishimura
