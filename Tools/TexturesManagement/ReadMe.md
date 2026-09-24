## Directories

Note: Tools work, but file structure is **VERY unstable**.

---

- `!_Textures_ALL_PICTURES`

Copy of Vectorier assets textures directory without `.meta` files. 

- **`!_Textures_ALL_PICTURES_UNROTATED`**

Self-explanatory. **Use it in [`DZIP Packer`](a_DZIP_Packer).**

- `!_Textures_META`

Folder containing `!_Textures_ALL_PICTURES` files' `.meta` counterparts. To use with `a_metaFinder.pyw`

- `!_Textures_Original`

Textures that came in original Vectorier version, as of 24rd July 2024.

- [`a_DZIP_Packer`](a_DZIP_Packer)

- [`b_XML_Parser`](b_XML_Parser)

Tool to proces an image based on specifications provided in an XML file, extracting multiple sub-images, saving them individually.

- [`c_spriteSheetGenerator](c_SpriteSheetGenerator)

Tool used to recreate sprite-sheets image objects generation.

## Files

- `a_metaCopier`

Python tool that allows to copy `.meta` files to desired directory.

- `b_copyFromTextures`

Updates the `!_Textures_ALL_PICTURES` by removing it and copying the Vectorier assets Textures directory without `.meta` files.

> [!WARNING]
> Includes rotated variations too!

- `c_metaPivotReplacer`

Replaces the `alignment` parameter in `.meta` files in selected subdirectories from `0` to `1`.

- `d_spriteSheetGenerator`

Designed to create a sprite sheet and a corresponding `.plist` file from a directory of **static images with identical dimensions**.

- `e_InputLogger`

Python app allowing to log simple user inputs in the game to the text file.

Its main target is to be used accordingly with `VectorierUtils` `input_map_processor.cs` script.