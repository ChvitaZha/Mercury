using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using System.Linq;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public NNModel modelAsset; // 存储神经网络模型的资源
    private Model m_RuntimeModel; // 运行时模型
    private IWorker worker; // 用于执行模型推理的工作器
    public GameObject objPrefab; // obj物体
    private int modelInputHeight = 1024; // 模型期望的输入高度
    private int modelInputWidth = 704; // 模型期望的输入宽度

    private List<GameObject> createdBalls = new List<GameObject>(); // 保存已创建的球

    private WebCamTexture webCamTexture; // 摄像头纹理
    public Canvas canvas; // 引用 Canvas 对象

    //public GameObject plane; // 引用 Plane 对象

    public RawImage cameraFeed; // 用于显示摄像头画面的UI组件

    private void Start()
    {
        m_RuntimeModel = ModelLoader.Load(modelAsset); // 加载神经网络模型

        // 使用GPU后端创建工作器
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, m_RuntimeModel); // 创建工作器

        //使用CPU     worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, m_RuntimeModel); 

        // 初始化摄像头
        webCamTexture = new WebCamTexture();
        webCamTexture.Play();

        // 将摄像头画面纹理应用到RawImage组件上
        cameraFeed.texture = webCamTexture;

    }


    private void OnDestroy()
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
        }
    }

    private void Update()
    {
        if (webCamTexture.isPlaying) // 如果摄像头正在播放
        {
            Texture2D currentFrame = GetCurrentFrame(); // 获取当前摄像头帧
            if (currentFrame != null)
            {
                ClearBalls(); // 清除上一帧创建的球
                Predict(currentFrame); // 执行目标检测
            }
        }
    }

    public Texture2D GetCurrentFrame()
    {
        if (webCamTexture == null || !webCamTexture.isPlaying)
            return null;

        Texture2D tex = new Texture2D(webCamTexture.height, webCamTexture.width, TextureFormat.RGB24, false); // 宽高交换
        Color32[] pixels = webCamTexture.GetPixels32();
        Color32[] rotatedPixels = RotateTexture(pixels, webCamTexture.width, webCamTexture.height);

        tex.SetPixels32(rotatedPixels);
        tex.Apply();

        return tex;
    }

    private Color32[] RotateTexture(Color32[] pixels, int width, int height)
    {
        Color32[] rotatedPixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (width - x - 1) * height + y; // 逆时针旋转90度
                rotatedPixels[index] = pixels[y * width + x];
            }
        }
        return rotatedPixels;
    }


    private float nmsThreshold = 0.5f; // 非极大值抑制阈值

    public void Predict(Texture2D inputTex)
    {
        // Resize input texture to match model input size
        Texture2D resizedTex = ResizeTexture(inputTex, modelInputWidth, modelInputHeight);

        Tensor inputTensor = new Tensor(resizedTex, channels: 3);
        try
        {
            worker.Execute(inputTensor);
            Tensor outputTensor = worker.PeekOutput();
            try
            {
                List<(float confidence, Vector2 boxCenter, Vector2 boxSize)> detections = new List<(float, Vector2, Vector2)>();

                for (int boxIndex = 0; boxIndex < outputTensor.width; boxIndex++)
                {
                    float confidence = outputTensor[0, 0, boxIndex, 4];
                    if (confidence > 0.5f)
                    {
                        Vector2 boxCenter = new Vector2(outputTensor[0, 0, boxIndex, 0], outputTensor[0, 0, boxIndex, 1]);
                        Vector2 boxSize = new Vector2(outputTensor[0, 0, boxIndex, 2], outputTensor[0, 0, boxIndex, 3]);
                        detections.Add((confidence, boxCenter, boxSize));
                    }
                }

                // 执行非极大值抑制
                List<(float, Vector2, Vector2)> nmsDetections = PerformNonMaximumSuppression(detections);

                ClearBalls(); // 清除上一帧创建的球

                // 创建新的球
                foreach (var (confidence, boxCenter, boxSize) in nmsDetections)
                {
                    CreateBall(boxCenter, boxSize);
                }
            }
            finally
            {
                outputTensor.Dispose();
            }
        }
        finally
        {
            inputTensor.Dispose();
        }
    }



    private List<(float, Vector2, Vector2)> PerformNonMaximumSuppression(List<(float, Vector2, Vector2)> detections)
    {
        List<(float, Vector2, Vector2)> nmsDetections = new List<(float, Vector2, Vector2)>();
        List<(float, Vector2, Vector2)> sortedDetections = detections.OrderByDescending(d => d.Item1).ToList(); // 按置信度降序排序

        while (sortedDetections.Count > 0)
        {
            (float confidence, Vector2 boxCenter, Vector2 boxSize) = sortedDetections[0];
            nmsDetections.Add((confidence, boxCenter, boxSize));
            sortedDetections.RemoveAt(0);

            for (int i = sortedDetections.Count - 1; i >= 0; i--)
            {
                (float otherConfidence, Vector2 otherBoxCenter, Vector2 otherBoxSize) = sortedDetections[i];
                float iou = CalculateIoU(boxCenter, boxSize, otherBoxCenter, otherBoxSize);
                if (iou > nmsThreshold)
                {
                    sortedDetections.RemoveAt(i);
                }
            }
        }

        return nmsDetections;
    }

    private float CalculateIoU(Vector2 box1Center, Vector2 box1Size, Vector2 box2Center, Vector2 box2Size)
    {
        float x1 = box1Center.x - box1Size.x / 2;
        float y1 = box1Center.y - box1Size.y / 2;
        float x2 = box1Center.x + box1Size.x / 2;
        float y2 = box1Center.y + box1Size.y / 2;

        float x3 = box2Center.x - box2Size.x / 2;
        float y3 = box2Center.y - box2Size.y / 2;
        float x4 = box2Center.x + box2Size.x / 2;
        float y4 = box2Center.y + box2Size.y / 2;

        float left = Mathf.Max(x1, x3);
        float right = Mathf.Min(x2, x4);
        float bottom = Mathf.Max(y1, y3);
        float top = Mathf.Min(y2, y4);

        float interArea = Mathf.Max(0, right - left) * Mathf.Max(0, top - bottom);
        float box1Area = (x2 - x1) * (y2 - y1);
        float box2Area = (x4 - x3) * (y4 - y3);
        float unionArea = box1Area + box2Area - interArea;

        return interArea / unionArea;
    }

    // 创建球的方法
    void CreateBall(Vector2 pos, Vector2 size)
    {
        float maxSize = Mathf.Max(size.x, size.y);
        Vector2 newSize = new Vector2(maxSize, maxSize);

        // 设置x、y的偏移量
        pos += new Vector2(-300f, -450f);

        GameObject newObj = Instantiate(objPrefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity); // 实例化OBJ预制体
        newObj.transform.localScale = new Vector3(newSize.x * 0.3f, newSize.y * 0.3f, newSize.x * 0.3f); // 将大小缩小到原来的0.5倍
        newObj.transform.SetParent(canvas.transform, false); // 设置父物体为 Canvas，保持世界空间不变
        createdBalls.Add(newObj); // 将新创建的OBJ添加到列表中
    }

    // 清除上一帧创建的球
    void ClearBalls()
    {
        foreach (GameObject obj in createdBalls)
        {
            Destroy(obj); // 销毁之前创建的OBJ
        }
        createdBalls.Clear(); // 清空列表
    }

    // Resize input texture to match model input size
    Texture2D ResizeTexture(Texture2D inputTex, int targetWidth, int targetHeight)
    {
        Texture2D resizedTex = new Texture2D(targetWidth, targetHeight);
        Graphics.ConvertTexture(inputTex, resizedTex);
        return resizedTex;
    }
}
