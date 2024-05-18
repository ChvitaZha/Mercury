import cv2
import numpy as np
from ultralytics import YOLO
import open3d as o3d

# 加载YOLO模型
model = YOLO('best.pt')

# 在图像上进行目标检测
img = cv2.imread('test.jpg')
results = model(img)
boxes = results[0].boxes.xyxy.cpu().numpy()

# 加载3D模型
mesh = o3d.io.read_triangle_mesh('3dmodel.obj')

# 创建Open3D渲染器
renderer = o3d.visualization.rendering.OffscreenRenderer(img.shape[1], img.shape[0])
renderer.scene.add_geometry('model', mesh)

# 设置渲染器参数
renderer.scene.camera.look_at([0, 0, 0], [0, 0, 1], [0, -1, 0])
renderer.scene.scene.enable_sun_light(True)

# 渲染3D模型
for i, box in enumerate(boxes):
    if results[0].boxes.conf[i] > 0.5:
        x1, y1, x2, y2 = [int(coord) for coord in box]
        cx = int((x1 + x2) / 2)
        cy = int((y1 + y2) / 2)
        side_length = int((x2 - x1 + y2 - y1) / 4)

        # 设置模型位置和大小
        mesh.translate((cx, cy, 0))
        mesh.scale(side_length, center=mesh.get_center())

        # 渲染场景
        rendered = renderer.render()

        # 将渲染结果合并到原始图像
        img[rendered[:, :, 3] > 0] = rendered[:, :, :3][rendered[:, :, 3] > 0]

# 显示结果图像
cv2.imshow('Result', img)
cv2.waitKey(0)
cv2.destroyAllWindows()
