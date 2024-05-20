# AnimefanPostUPs-Tools
 
# Easy Window:
![image](https://github.com/AnimefanPostUP/Unity.Animefans_Tools/assets/93488236/7d86e98d-28fb-4d81-9ebb-684a145e99ae)

## Functions
find all your Textures easly 
- Only the Folders you really need 
- Drag and Drop your Folder Over the Folder Panel on the Left side to Add them
- Drag Elements from the Project view onto the Right Side and Move them Into the Folder
- Drag and Drop them out the Window to apply them to your Materials
- Shift D to duplicate your Textures etc and what else (does not duplicate the used Textures itself of mats YET!)
- Use Alt to open the Folder or File in your Windows Explorer
- Control to Focus the File in the Project View
- Alt + Control +Shift to open the Delete Menu


## Hide and Show Icons
- Set the Fontsizes
- Shortened Names when Element size gets smaller
- Fixed Scrolling
- Better Caching of Preview Images
- Adjustable Preview Size for Performance

</br></br></br>
</br></br>
## Audio Timeline Editor:
![image](https://github.com/AnimefanPostUP/Unity.Animefans_Tools/assets/93488236/f69f0be1-26db-415d-b1ef-57d4c02981a9)


### Generally: Combine and Offset Audio Clips!

</br></br></br>


## Timeline Area

![image](https://github.com/AnimefanPostUP/Unity.Animefans_Tools/assets/93488236/ee9c31f6-0125-4ab8-9ac8-e1ea02a13888)

### Drag and Drop
- **Drag and Drop** of AudioClips and the Json files
- **Drag the Clips** in the Timeline with Left Mouse


### Navigation:
- **Move/Scale View** (Alt + Mouseposition / Mousewheel) (Scaling disabled when Synch is On!)
- **Reset View** (Shift)
- **Scale View** (Control) (+)

</br></br></br>


## Timeline Tracks

![image](https://github.com/AnimefanPostUP/Unity.Animefans_Tools/assets/93488236/9fc1ef32-10e4-4f2f-af38-b5911dde0d36)

### Track Buttons
- **Change** Volume (Slider) (Multiplyer)
- **Exclude** Track from Render (Eye)
- **Remove** Track (X)
- **Reload** Track Data/Previews (2 Arrows)
- **Snap** Track to 0 (Yellow Arrow in a Box)

### Dragging:
- **Dragging Track:** (Left Mouse Drag)
- **Dragging Snap Start:** by holding Shift when Dragging
- **Dragging Snap End:** by holding Shift when Dragging

### Pinging:
- **Ping:** Audioclips (Tracks Sidepanel Label)
- **Ping:** Json,Sourcefolder, Render

- **Waveform** Raw Display of the Aplitudes, when Normalization is Enabled, the Red Color Shows the Normalized and Adjusted Data and the Gray is the Resampled unadjusted Data

</br></br></br>


## Dropdown Menu

![image](https://github.com/AnimefanPostUP/Unity.Animefans_Tools/assets/93488236/f5f7ef57-6f96-45df-abec-5ea7b25f0c5c)

### Save / Load / Create / Render:
- **Create** new Project
- **Save / SaveAs** Projects as Json
- **Load** the Jsons
- **Render / RenderAs** to save it as a WavFile
- **Backup** will save a Jsonfill with a Timestamp, usefull to make sure, that you dont Overwrite your Files
- **Quickrender** (CPU Icon on the Right Side of the Dropdown, does the same as the Render Button)
- **Load** the Jsons

### Extras:
- **Synch** will Synch the Windows Position etc. with the Animation Window, will also Synch the Playbutton with the Audio, if its Rendered

### Automatic Extras:
- The Addon Autoloads the Lastes Json on Startup (Searches for the _autosave first)

</br></br></br>


## Settings Menu

![image](https://github.com/AnimefanPostUP/Unity.Animefans_Tools/assets/93488236/08f90128-32b2-4ca2-a6d3-5aa2506d0494)


### Output Settings:
- Samplerate, Bitrate, Mono/Stereo

### Normalization for Input
- Used for Volume Multiplication / Gain
- Adjust Normalized Strength ([NormalizationModifier(ModifierGainInput, MaxAudio, Strength)] * Audiovalue * Gain )
- Toggle it On or Off (Default ON!)
- 
### Normalization for Output
- Used for Volume Multiplication / Gain
- Adjust Normalized Strength ([NormalizationModifier(MaxAudio, Strength)] * Audiovalue )
- Toggle it On or Off (Default ON!)

### Extra Settings:
- **Auto Save Json** (Saves the Project as Name_autosave.json)
- **Link Sidepanel** (Will align the Vertical Scroll of the Tracks with the Sidepanel Tracks)
- **Optimized Build** (Experimental Setting that will try to only render the Changed Parts of the Audio (may not be accurate when used a lot))
- **Auto Build** (Updates and Renders after Changes happen to Settings, Volume, Trackposition etc., can draw quite a bit of Performance, Helpfull for Small Projects)

- **Save** (Will Apply all Settings to the Project)


</br></br></br>


## Notes
- The Filesize of Wav Files can Explode Rapidly, you may Adjust the Quality and allow Unity to Change the Samplerate/Optimizing it

![image](https://github.com/AnimefanPostUP/Unity.Animefans_Tools/assets/93488236/9c483b34-296d-4901-b8e8-3e0febc2ca56)



