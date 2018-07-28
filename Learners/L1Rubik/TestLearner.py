import Shared.Globals
import Shared.RubikLoss
from Shared.DataGenerator import DataGenerator
from Shared.ModelCreator import ModelCreator

import keras
import time
from os import path

train_data_dir = Shared.Globals.get_subdir("Rubik/Only Rubik")
train_labels_dir = Shared.Globals.get_subdir("Rubik/Only Rubik/Bounds")
models_dir = Shared.Globals.get_subdir("Rubik/Models/L1Rubik")
img_width, img_height = 299, 299
nb_labels = 14
batch_size = 32
epochs = 500
samples_multiplier = 2

weights_file_name = path.join(models_dir, "1807271415-VGG16-SGD.h5")
load_full_model = False

data_generator = DataGenerator.init_from_folder(train_data_dir, train_labels_dir, batch_size=batch_size,
                                                target_width=img_width, target_height=img_height,
                                                label_flip_pairs=[(2, 6), (3, 5)],
                                                label_extra_normalization=Shared.RubikLoss.correct_label_orientation)
validation_data = data_generator.get_validation_data()

steps_per_epoch = len(data_generator) * samples_multiplier
steps_per_epoch_validation = len(validation_data)

Shared.RubikLoss.register_losses()

model_creator = ModelCreator(image_width=img_width, image_height=img_height, nb_labels=nb_labels)
if load_full_model:
    model = model_creator.load(weights_file_name)
else:
    model = model_creator.get_model(ModelCreator.VGG16,
                                    weights_file_name=weights_file_name,
                                    loss_function=Shared.RubikLoss.cube_loss3)

time_stamp = time.strftime("%y%m%d%H%M") + \
             "-" + model_creator.base_model_name + \
             "-" + model.optimizer.__class__.__name__

checkpoint_file = path.join(path.join(models_dir, "Checkpoints"), time_stamp + ".h5")


def fit_model(override_epochs=-1):
    tensor_board = keras.callbacks.TensorBoard(log_dir="./Logs/" + time_stamp, histogram_freq=0, write_graph=False)
    reduce_lr = keras.callbacks.ReduceLROnPlateau(monitor="loss", factor=0.5, patience=10, cooldown=1, verbose=1)
    checkpoint = keras.callbacks.ModelCheckpoint(filepath=checkpoint_file, verbose=1,
                                                 save_best_only=True, save_weights_only=True)
    callbacks = [tensor_board, reduce_lr, checkpoint]

    model.fit_generator(data_generator, steps_per_epoch=steps_per_epoch,
                        epochs=epochs if override_epochs < 1 else override_epochs,
                        validation_data=validation_data, validation_steps=steps_per_epoch_validation,
                        callbacks=callbacks)


if not load_full_model:
    # fit_model(override_epochs=2)

    # model_creator.unfreeze_top(model)
    # fit_model(override_epochs=10)

    model_creator.unfreeze_top(model, from_level=0, new_lr=0.0001)
fit_model()


# """ Save model
model.load_weights(checkpoint_file)  # restore weights for best validation checkpoint
ModelCreator.save(model, path.join(models_dir, time_stamp + ".h5"))
ModelCreator.save_tf(model, models_dir, time_stamp + ".pb")
# """


# """ Visualize predictions
import Visualizer
images, labels = validation_data.__getitem__(0)
Visualizer.show_predictions(images, model.predict(images))
# """
