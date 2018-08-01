import os
import shutil
import Shared.Globals

data_dir = Shared.Globals.get_subdir("Rubik/Only Rubik")
labels_dir = Shared.Globals.get_subdir("Rubik/Only Rubik/Bounds")
processed_dir = Shared.Globals.get_subdir("Rubik/Only Rubik/Processed")

for image_file in os.listdir(data_dir):
    if image_file.endswith(".png"):
        label_file = os.path.splitext(image_file)[0] + ".bounds"
        if os.path.isfile(os.path.join(labels_dir, label_file)):
            if not os.path.isfile(os.path.join(processed_dir, image_file)):
                shutil.copy(os.path.join(data_dir, image_file), processed_dir)
