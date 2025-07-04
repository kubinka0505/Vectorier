# **DZIP Packer** 🗃️

A tool that allows to perform transformations on images and pack them into `.dz` archive file.

**Intended usage is to modify all of them, not a single directory,** unless possible.

Features:
- Operating on backup directory (original files are untouched)
- **Embedded** external image optimization executables:
  - [`PNGQuant`](https://github.com/kornelski/pngquant), for `PNG` images.
  - [`JPEGOptim`](https://github.com/tjko/jpegoptim), for `JPG`/`JPEG` images.
- 2 archive compression algorithms:
  - `dz`: For small amount of files. **Unstable!**
  - `zlib`: For big amount of files.
- Safe limitation of image colors (palette change)

---

## Versions

Both versions have broken file counter.

1. `old`
   - Allows for individual images rotations by degrees **up to decimal places**.
1. `new`
   - Precise rotations have been replaced by `PIL.Image.Transpose` components, because project releases published after ca. 13rd June 2024 allow rotations inside editor. However, no solutions for internal `X`/`Y` sprite transforming and its errors have been found.

---

## Configuration files 📝

Stored inside `json` directory.

- `image_variations.json`<sup>1</sup>: File with object containing **transformed file base name suffix** and **type of transformation**, being a variable of `PIL.Image.Transpose` class. **Rotation-named variables are counter-clockwise**<sup>2</sup>. **Cannot be integers**.
- `default_exclusions.json`: In this file, files that base name **contain** certain strings are **not transformed**.
- `directory_exclusions.json`: Directory to skip from processing, but to include in final output. (non-transformed)

---

## Warning ⚠️
- Perfect method for this task does not exist and has to be achieved by trial and error method — **if archive creation fails despite the selected algorithm, only way to make it work again is to exclude files from it** (by moving/deleting them out of input directory or adding them to `json/default_exclusions.json`) **or remove variations of pictures**. (`json/image_variations.json`)

---

<sub><sup>1</sup> - new version only</sub>
<sub><sup>2</sup> - [C source](https://github.com/python-pillow/Pillow/blob/main/src/_imaging.c#L2107-L2163)</sub>