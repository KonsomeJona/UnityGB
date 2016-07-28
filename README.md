# UnityGB

UnityGB allows you to run a Game Boy ROM inside your Unity project.
It has been written in C# and is only using Mono allowing you to export the project to any platform supported by Unity.

UnityGB became the main subject of several articles like [CNET, Gameboy emulator on the Oculus Rift: Gameception](http://www.cnet.com/news/gameboy-emulator-on-the-oculus-rift-gameception/).

UnityGB is still in alpha version, many bugs and glitches can occur. Also, the code is far from being perfect and needs a lot of optimization. Any and all help welcome! Please feel free to contribute to this project.

Please also take some time to take a look at our other applications on our website [Takohi.com](http://www.takohi.com) and our other [Unity assets](https://www.assetstore.unity3d.com/en/?gclid=CjwKEAjwuoOpBRCSy6yQm66J1g8SJABrXW48qCgi3rfBrvrAQu55uQeS0U3YH51O-Ybf6N1ZDwJVQRoCqBrw_wcB#!/search/page=1/sortby=popularity/query=publisher:4241). :+1:

## Compatibilities

### What is supported

* Game Boy games with cartridge <= MBC5
* Input
* Sound
* Save (save files are compatible with Visual Boy Advance and other emulators)

### Planned features

* Game Boy Color compatibility
* Link Cable
* Oculus Rift

### Known issues

* Sound synchronization
* Pokemon Yellow: Pikachu voices are not played
* Some glitches with sprites order

## Demonstrations

**Hilarious Gameboy Emulator with Leap Motion in Unity**, by [Pierce Wolcott](https://piercewolcott.com/)  
[![Gameboy Emulator with Leap Motion in Unity](https://i.vimeocdn.com/video/514669012_590x332.jpg)](https://vimeo.com/124805471 "Leap Boy")

**Oculus Rift Gameboy Emulator in Unity**, by [Shane O'Brien](http://www.youtube.com/watch?v=wby8pMrYYaM)  
[![Oculus Rift Gameboy Emulator in Unity](http://img.youtube.com/vi/wby8pMrYYaM/0.jpg)](http://www.youtube.com/watch?v=wby8pMrYYaM "Oculus Rift Gameboy Emulator in Unity")

**UnityGB4BB10, GameBoy Emulator for BlackBerry 10**  
[![UnityGB4BB10](http://www.filearchivehaven.com/wp-content/uploads/2014/08/UnityGB4BB-Games-1024x576.png)](http://www.filearchivehaven.com/2014/08/17/proud-to-announce-another-gameboy-emulator-for-blackberry-10-unitygb4bb10/ "UnityGB4BB10")

**Official Web Demo**, by [Takohi](http://www.takohi.com)  
[![Official Demo](https://bitbucket.org/repo/8MjKzK/images/2954418396-unitygb_demo_screenshot_2.png)](http://www.takohi.com/data/unity/unitygb/ "Official Demo")

## Usage
**UnityGB** can work in several ways. What you have to do is to make your own implementation about how to manage inputs (controls) and outputs (video and sound).
In the Unity package and the sources, you will already find one classic scene outputing the video in a classic texture, the audio trough the [OnAudioFilterRead](http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.OnAudioFilterRead.html) method, and joypad input from the keyboard.

### Joypad

An example of how to implement the controls can be found in *DefaultEmulatorManager.cs*.

When you want to emulate an input from the player into the game, you just have to call the *SetInput* method from the emulator reference:
```
void SetInput(Button button, bool pressed);
```
Button is an enum with these values:

```
public enum Button {Up, Down, Left, Right, A, B, Start, Select};
```
For example, if you want to press the button start when the player is hitting the space button:

```
if(Input.GetKeyDown(KeyCode.Space))
    Emulator.SetInput(EmulatorBase.Button.Start, true);
else if(Input.GetKeyUp(KeyCode.Space))
    Emulator.SetInput(EmulatorBase.Button.Start, false);
```

### Video

An example of how to implement the video can be found in *DefaultVideoOutput.cs*.

In order to output the video from the emulator or in other words, to display the Game Boy screen, you will have to make your own class implementing the *IVideoOutput* interface.

This interface has only two methods:

```
void SetSize(int w, int h);
void SetPixels(uint[] colors);

```

The *SetSize* method will be called after the rom is loaded. It will tell you what is the size of the screen in pixels (160x144, it should never change) so you can initialize your way to display.

The method *SetPixels* will be called every time the frame has been updated. As parameter, an array of colors that corresponds to the color of each pixel. The size of the array will be *width x height*.

Here is a method to convert a uint color into a Unity color:

```
private Color UIntToColor(uint color)
{
    byte a = (byte)(color >> 24);
    byte r = (byte)(color >> 16);
    byte g = (byte)(color >> 8);
    byte b = (byte)(color >> 0);
    return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
}
```

### Audio

An example of how to implement the audio can be found in *DefaultAudioOutput.cs*.

With Unity, the only way to dynamically produce audio is by using the method [OnAudioFilterRead(float[] data, int channels)](https://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.OnAudioFilterRead.html) from a *MonoBehaviour* object.

In the package, we already provide a way to produce the audio trough the *DefaultAudioOutput* class. If you want to use it, just attach that script to a game object and add to it an *AudioSource* component.

If you still want to make your own implementation for outputing audio, you will have to make a class that implements the interface *IAudioOutput*.

This interface has three methods:
```
int GetOutputSampleRate();
int GetSamplesAvailable();
void Play(byte[] data, int offset, int count);
```

* *GetOutputSampleRare()* has to return the sample rate (44100Hz, ...).
* *GetSamplesAvailable()* returns the number of samples to return during the next step.
* *Play(byte[] data)* gives you the samples to play.

Samples are interleaved (%0 = Left, %1 = Right). This means for each sample: data[i] = left sample, data[i + 1] = right sample.

### Save

An example of how to manage save files can be found in *DefaultSaveMemory.cs*.

In order to manage save files, you will have to make your own class implementing the *ISaveMemory* interface.

This interface has only two methods:

```
void Save(string name, byte[] data);
byte[] Load(string name);
```

The *Save* method is called when the user stops the game, and the *Load* method is called when the game is loaded. The *name* parameter is the name of the game that is currently played.

Globally, when the *Save* method is called, you simply have to store somewhere the byte array given as parameter, then load and return this same byte array when the *Load* method is called for the same game.

### Make the emulator run

An example of how to use the emulator can be found in *DefaultEmulatorManager.cs*.

The code below is a simple way showing how to initialize and make run UnityGB with a *MonoBehaviour* script.

First, we create an instance of the emulator and load the ROM in the *Start* method.

```
void Start()
{
    // Load emulator
    IVideoOutput drawable = ...; // Reference to your IVideoOutput implementation.
    IAudioOutput audio = ...; // Reference to your IAudioOutput implementation.
    ISaveMemory saveMemory = ...; // Reference to your ISaveMemory implementation.
    Emulator = new Emulator(drawable, audio, saveMemory); // Instantiate emulator.

    byte[] rom = ...; // Load ROM binary.
    Emulator.LoadRom(www.bytes); // Load ROM into emulator.
}
```
Then we make the emulator running during the *Update* method of the *MonoBehaviour*. We also use this method to manage inputs from the user and send them to the emulator.

```
void Update()
{
    // Input
    if(Input.GetKeyDown(KeyCode.Space))
        Emulator.SetInput(EmulatorBase.Button.Start, true);
    else if(Input.GetKeyUp(KeyCode.Space))
        Emulator.SetInput(EmulatorBase.Button.Start, false);
    ...

    // Run
    Emulator.RunNextStep();
}
```

Don't forget to attach the previous MonoBehaviour to a game object in order to make it run.

## Reference Material

* [Official UnityGB thread on Unity forum](http://forum.unity3d.com/threads/245974-unityGB-Emulator-Game-Boy-for-Unity-RELEASED)
* http://meatfighter.com/gameboy/
* http://www.millstone.demon.co.uk/download/javaboy/
