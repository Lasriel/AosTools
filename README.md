<h1 align="center">AosTools</h1>

<div align="center">
  
  A command line tools for extracting data from, and repacking data back to .aos archives.

  &nbsp;<img src="https://img.shields.io/badge/dotnet-3.1.426-purple" />
</div>


&nbsp;

The .aos file format is used by visual novel producers [Princess Sugar](https://vndb.org/p1781), [Sugar Pot](https://vndb.org/p511) and [LiLiM | リリム](https://vndb.org/p281) with their in-house engine.

This tool supports specifically version 2 of the aos format which should cover any visual novel made after 2006.

## Supported Formats

| Extension | Support         | Description                                                                                                                                                                        |
| --------- | --------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| .aos      | Full support    | Archive file, can be extracted and repacked.                                                                                                                                       |
| .scr      | Full support    | Script file, Can be decoded to Shift-JIS encoded text file and encoded back to scr file. The engine does not support any other encoding format.                                    |
| .abm      | Partial support | Compressed bitmap image/animated bitmap, Can only be decoded to a bitmap file(s). For animated bitmaps .json file is created alongside the animation that includes the frame data. |
| .msk      | Partial support | Grayscale mask, Normal 8-bit bitmap file that is not encoded, only the extension is changed during decode. Used for scene transitions and similar effects.                         |

## Usage
```
AosTools command <input file or folder> [options] <output folder>
```

| Command    | Options        | Description                                                  |
| ---------- | -------------- | ------------------------------------------------------------ |
| help       |                | Prints help message with all the available commands.         |
| extract    | --nodecode     | Extracts all files from an .aos archive file.                |
| decode     |                | Decodes a file or a directory of encoded scr/ABM files.      |
| repack     | --noencode     | Repacks all files in a directory to an .aos archive.         |
| encode     |                | Encodes a file or a directory of txt files.                  |

## License

This project is published under [MIT License](/LICENSE).
