# WoWTools.Minimaps
Various projects related to extracting/compiling minimaps from World of Warcraft into larger images.

You can download the latest compiled release __[here](https://github.com/Marlamin/WoWTools.Minimaps/releases)__.

## Extraction
To extract minimap tiles from the game, you can use any tool that can extract files from the game, but for speed and/or automation the WoWTools.MinimapExtract tool is included. 
### Arguments
`WoWTools.MinimapExtract.exe <product> <output folder> (wowPath) (mapFilter)`

`<product>` is the WoW product to extract from. You can find a list of products [here](https://wowdev.wiki/TACT#Products).

`<output folder>` the folder to extract files to. If you specify e.g.`out` it will extract to the same directory as `WoWTools.MinimapExtract.exe` in a subfolder called out.

`(wowPath)` optional argument to specify WoW installation directory. Be sure to specify the directory that contains a "Data" folder, not something like `_retail_`. If not specified, it will stream files from CDN which is slower.

`(mapFilter)` optional argument to only extract a single map. You can view a list of maps on [wago.tools](https://wago.tools/db2/Map) or [wow.tools](https://wow.tools/dbc/?dbc=map), in both cases the value from the Directory column is used. If not specified, all maps are extracted.

### Examples
Extract all minimaps from the `wowt` (Retail PTR) product into folder `out`, streaming files from CDN (slower).  
```WoWTools.MinimapExtract.exe wowt out```

Extract all minimaps from the `wowt` (Retail PTR) product into folder `out`, using locally installed files from the `C:\World of Warcraft` directory (faster).  
```WoWTools.MinimapExtract.exe wowt out "C:\World of Warcraft"```

Extract only map 2444 (Dragon Isles) minimaps from the `wowt` (Retail PTR) product into folder `out`, using locally installed files from the `C:\World of Warcraft` directory.  
```WoWTools.MinimapExtract.exe wowt out "C:\World of Warcraft" 2444```

Extract only map 2444 (Dragon Isles) minimaps from the `wowt` (Retail PTR) product into folder `out`, streaming files from CDN.  
```WoWTools.MinimapExtract.exe wowt out "" 2444```
## Compilation
To compile minimaps into a giant single PNG you can use the WoWTools.MinimapCompile tool, keep in mind depending on hardware and the size of the map, this may take up to a few minutes.
### Arguments
`WoWTools.MinimapCompile.exe <input folder> <output PNG> (resolution: 256, 512 or 1024)`

`<input folder>` is the folder that has the extracted map_`xx`_`yy`.blp files. 
`<ouput PNG>` is the PNG file the resulting image should be saved to.
`(resolution)` optional/advanced: expected resolution of each minimap tile. 512 by default for modern maps but can also be 256 (older maps) or 1024 (not seen in official minimaps). If resolution is 512 but an input tile is 256x256 it is upscaled, vice versa if resolution is 256 but an input tile is 512x512 it is downscaled. Tiles of 1024x1024 resolution are untouched.

### Examples
Compile map 2444 from folder `out\world\minimaps\2444` to `Dragon Isles.png`.  
`WoWTools.MinimapCompile.exe "out\world\minimaps\2444" "Dragon Isles.png"`.

## Other tools
These tools are __not supported__ and as such not included in releases.
### WoWTools.MinimapCut
Advanced tool to cut compiled minimap images into tiles for Leaflet.
### WoWTools.MinimapCompileWMO
Advanced tool to compile WMO minimaps.
### WoWTools.MinimapTranslate
Advanced tool to rename minimaps based on old md5translate.trs files.