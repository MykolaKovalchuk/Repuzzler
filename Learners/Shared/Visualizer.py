import matplotlib.pyplot as plt
import cv2


def show_history(history, title):
    plt.plot(history.history["mean_absolute_error"][1:])
    plt.title(title)
    plt.ylabel("mean absolute error")
    plt.show()


def show_image(img, lbl, title=None):
    img_height = img.shape[0]
    img_width = img.shape[1]

    def get_point(index):
        x = int(lbl[index * 2] * img_width)
        y = int(lbl[index * 2 + 1] * img_height)
        return x, y

    color = (0.0, 0.0, 1.0)

    cv2.line(img, get_point(0), get_point(1), color, thickness=2)
    cv2.line(img, get_point(0), get_point(3), color, thickness=2)
    cv2.line(img, get_point(0), get_point(5), color, thickness=2)
    cv2.line(img, get_point(1), get_point(2), color, thickness=2)
    cv2.line(img, get_point(2), get_point(3), color, thickness=2)
    cv2.line(img, get_point(3), get_point(4), color, thickness=2)
    cv2.line(img, get_point(4), get_point(5), color, thickness=2)
    cv2.line(img, get_point(5), get_point(6), color, thickness=2)
    cv2.line(img, get_point(6), get_point(1), color, thickness=2)

    img = img[..., ::-1]

    plt.imshow(img)
    if title:
        plt.title(title)
    plt.show()


def show_predictions(images, predictions):
    for i in range(len(images)):
        show_image(images[i], predictions[i])
