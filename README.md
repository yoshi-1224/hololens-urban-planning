# Hololens

**Notes**:
- All the codes in the Microsoft Hololens Academy are based on the older version of HoloToolkit that used to include classes such as `GestureManager` and `InteractibleManager`. These classes are now combined into `InputManager`. For more info, take a look at this link <https://github.com/Microsoft/HoloToolkit-Unity/tree/master/Assets/HoloToolkit/Input>

- `KeywordManager` is going to be deprecated. Instead, the use of `InputSpeechSource` is recommended. Voice recognition is only enabled when the object with this script attached is in focus. We can also use it as global keyword manager by attaching it to `InputManager` and registering it as global listener.