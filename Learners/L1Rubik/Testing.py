import cv2
import matplotlib.pyplot as plt

imageBGR = cv2.imread('d:/Rubik/Hands/Left/i-02003.png')
print(type(imageBGR))
print(imageBGR.shape)

image = cv2.cvtColor(imageBGR, cv2.COLOR_BGR2RGB)
imgplot = plt.imshow(image)
plt.show()

# cv2.imshow('Test image', image)
# cv2.waitKey(0)
# cv2.destroyAllWindows()
