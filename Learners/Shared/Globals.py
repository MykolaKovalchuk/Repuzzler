import os

root_dir = "/mnt/DA92812D92810F67"
#root_dir = "d:/"


def get_subdir(subdir):
    return os.path.join(root_dir, subdir)
