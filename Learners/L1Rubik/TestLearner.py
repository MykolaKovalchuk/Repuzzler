from Shared.DataGenerator import DataGenerator
from Shared.ModelCreator import ModelCreator

img_width, img_height = 299, 299
train_data_dir = "/mnt/DA92812D92810F67/Rubik/Only Rubik"
train_labels_dir = "/mnt/DA92812D92810F67/Rubik/Only Rubik/Bounds"
nb_labels = 14
batch_size = 8
steps_per_epoch = 100
epochs = 20

model_creator = ModelCreator(image_width=img_width, image_height=img_height, nb_labels=nb_labels)
model = model_creator.get_model()

data_generator = DataGenerator(train_data_dir, train_labels_dir,
                               batch_size=batch_size,
                               target_width=img_width, target_height=img_height)
data_generator.flip_label_pairs.append((2, 6))
data_generator.flip_label_pairs.append((3, 5))

model.fit_generator(data_generator, steps_per_epoch=steps_per_epoch, epochs=epochs)

model_creator.unfreeze_top(model)

model.fit_generator(data_generator, steps_per_epoch=steps_per_epoch, epochs=epochs)

for idx in range(data_generator.__len__()):
    images, labels = data_generator.get_item(idx, False)
    predictions = model.predict(images)
    for i in range(batch_size):
        print("==============================================================")
        for j in range(nb_labels):
            print("  " + str(labels[i, j]) + "\t:  " + str(predictions[i, j]))
