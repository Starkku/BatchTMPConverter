# BatchTMPConverter

BatchTMPConverter is a command-line tool for replacing tile image data on Command & Conquer Tiberian Sun / Red Alert 2 terrain template (tmp) files. It allows for fast and quick conversion of PNG image files into already existing terrain templates.

Download for latest build (automatically generated from latest commit in `master` branch) can be found [here](https://github.com/Starkku/BatchTMPConverter/releases/tag/latest).

Accepted parameters:
```
-h, --help                          Show help.
-i, --files=VALUE                   A comma-separated list of input file(s) and/or directory/directories.
-p, --palette=VALUE                 Palette file to use for conversion.
-o, --output-images                 Output template data as images instead of  converting images to templates.
-e, --extensions-override=VALUE     Comma-separated list of file extensions (including the .) to use instead of built-in defaults.
-r, --replace-radarcolor            Alter tile radar colors based on new image & palette data.
-m, --radarcolor-multiplier=VALUE   Multiplier to radar color RGB values, if they are altered.
-x, --extraimage-bg-override        Allow overwriting background color pixels on existing extra images.
-c, --accurate-color-matching       Enables slower but more accurate palette color matching.
-d, --preprocess-commands=VALUE     List of commands to use to preprocess images before conversion. Comma-separated list of commands consisting of executable and arguments separated by semicolon.
-b, --no-backups                    Disable backing up the edited files with same name using file extension .old.
-f, --processed-files-log=VALUE     Filename to write timestamps of processed files to. Files with matching filenames and unchanged timestamps will not be processed again.
-l, --log-to-file                   Write log info to file as well as console.
```

## Acknowledgements

BatchTMPConverter uses code from the following open-source projects to make its functionality possible.

* NDesk.Options: http://www.ndesk.org/Options

In addition, knowledge from Olaf van der Spek's documentation on TS/RA2 TMP file format was instrumental in implementation of this tool.

## License

This program is licensed under GPL Version 3 or any later version.

See [LICENSE.txt](LICENSE.txt) for more information.
