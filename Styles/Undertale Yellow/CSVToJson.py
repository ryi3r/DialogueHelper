# This script allows you to convert the .txt files in the current
# Directory to .json (using the UTMT Scripts to export the .txt files)

import os, json

def main():
    for (roots, _, files) in os.walk("."):
        if roots != ".":
            continue
        for file in files:
            if file.endswith(".txt"):
                print("Converting", file)
                data = {
                    "Name": "NoName",
                    "Texture": "NoTexture.png",
                    "Size": 0,
                    "Ascender": 0,
                    "AscenderOffset": 0,
                    "Glyphs": []
                }
                with open(file) as f:
                    for index, line in enumerate(f.readlines()):
                        line = line.split(";")
                        if index == 0:
                            data["Name"] = line[0].replace("\"", "")
                            data["Texture"] = file.replace(".txt", ".png")
                            data["Size"] = int(line[1])
                            data["Ascender"] = int(line[8])
                            data["AscenderOffset"] = int(line[9])
                        else:
                            data["Glyphs"].append({
                                "Char": chr(int(line[0])),
                                "X": int(line[1]),
                                "Y": int(line[2]),
                                "Width": int(line[3]),
                                "Height": int(line[4]),
                                "Shift": int(line[5]),
                                "Offset": int(line[6]),
                            })
                            pass
                        pass
                    f.close()
                with open(file.replace(".txt", ".json"), "w") as f:
                    f.write(json.dumps(data, indent = 4))
                    f.close()

main()