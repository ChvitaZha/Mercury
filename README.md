# MercuryAR

![alt text](image.png)

一个基于Unity开发的，可识别特定物体并渲染模型的AR程序。


各程序版本如下：

YOLO版本：YOLOv8  https://github.com/ultralytics/ultralytics

模型版本：ONNX 10

Unity版本：2022.3.23f1

barracuda版本：3.0.1  https://docs.unity3d.com/Packages/com.unity.barracuda@3.0/manual/index.html


备注：

YOLO文件夹中，存放着所有训练、预测、导出代码，相关依赖可见官方文档：https://docs.ultralytics.com/zh

除YOLO外，其它文件夹为Unity项目文件夹，可导入Unity中使用。其中，ObjectDetection.cs为项目代码，具体逻辑见注释。

若导入自用ONNX模型，可选择版本9或10，其它版本barracuda可能会报错