import Globals
import Visualizer
import RubikLoss
from Shared.DataGenerator import DataGenerator
from Shared.ModelCreator import ModelCreator

import keras
import time


train_data_dir = Globals.get_subdir("Rubik/Only Rubik")
train_labels_dir = Globals.get_subdir("Rubik/Only Rubik/Bounds")
img_width, img_height = 299, 299
nb_labels = 14
batch_size = 16
epochs = 100

data_generator = DataGenerator(train_data_dir, train_labels_dir,
                               batch_size=batch_size,
                               target_width=img_width, target_height=img_height)
data_generator.flip_label_pairs.append((2, 6))
data_generator.flip_label_pairs.append((3, 5))

steps_per_epoch = len(data_generator) * 2

model_creator = ModelCreator(image_width=img_width, image_height=img_height, nb_labels=nb_labels)
model = model_creator.get_model(RubikLoss.cube_loss)
"""
for li in range(len(model.layers)):
    print(str(li) + " : " + model.layers[li].name)
# """

time_stamp = time.strftime("%y%m%d%H%M") + \
             "-" + model_creator.base_model_name + \
             "-" + model.optimizer.__class__.__name__


def fit_model(override_epochs=-1, unfreeze_top_from_level=-1):
    if unfreeze_top_from_level >= 0:
        model_creator.unfreeze_top(model, unfreeze_top_from_level)

    tensor_board = keras.callbacks.TensorBoard(log_dir="./Logs/" + time_stamp, histogram_freq=0, write_graph=False)
    reduce_lr = keras.callbacks.ReduceLROnPlateau(monitor="loss", factor=0.2, patience=5, cooldown=1, verbose=1)
    callbacks = [tensor_board, reduce_lr]

    history = model.fit_generator(data_generator,
                                  steps_per_epoch=steps_per_epoch,
                                  epochs=epochs if override_epochs < 1 else override_epochs,
                                  callbacks=callbacks)
    # Visualizer.show_history(history, time_stamp)


fit_model(override_epochs=10)
fit_model(unfreeze_top_from_level=12)
# fit_model(unfreeze_top_from_level=249)
# fit_model(unfreeze_top_from_level=105)
# fit_model(unfreeze_top_from_level=0)

# visualize images
images, labels = data_generator.get_validation_item(10)
Visualizer.show_predictions(images, model.predict(images / 255.0))
