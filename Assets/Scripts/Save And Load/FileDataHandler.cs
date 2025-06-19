using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MessagePack;
using Newtonsoft.Json;
using UnityEditor.PackageManager;
using UnityEngine;

//文件数据保存处理
public class FileDataHandler {
    private string dataDirPath = "";
    private string dataFileName = "";

    //加密
    private bool encryptData = false;
    private string codeWord = "alexdev";//密钥

    public FileDataHandler(string dataDirPath, string dataFileName, bool encryptData) {
        this.dataDirPath = dataDirPath;
        this.dataFileName = dataFileName;
        this.encryptData = encryptData;
    }

    //保存数据
    public void Save(MapData data) {
        string fullPath = Path.Combine(dataDirPath, dataFileName);
        try {
            
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            //string dataToStore = JsonConvert.SerializeObject(data);
            //for (int x = 0; x < 200; x++) {
            //    for (int y = 0; y < 200; y++) {
            //        Debug.Log(data.chunkDatas[0, 0][2, x, y]);
            //    }
            //}

            //if (encryptData)
            //    dataToStore = EncryptDecrypt(dataToStore);
            //using (FileStream stream = new FileStream(fullPath, FileMode.Create)) {
            //    using (StreamWriter writer = new StreamWriter(stream)) {
            //        writer.Write(dataToStore);
            //    }
            //}
            byte[] bytes = MessagePackSerializer.Serialize(data);
            byte[] compressed = Compress(bytes);

            File.WriteAllBytes(fullPath, compressed);
        } catch (Exception e) {
            Debug.Log("尝试保存数据错误：" + fullPath + "\n" + e);
        }
    }

    public MapData Load() {
        string fullPath = Path.Combine(dataDirPath, dataFileName);

        MapData loadData = null;
        Debug.Log("开始加载地图文件："+DateTime.Now);
        if (File.Exists(fullPath)) {
            try {
                string dataToLoad = "";
                using (FileStream stream = new FileStream(fullPath, FileMode.Open)) {
                    using (StreamReader reader = new StreamReader(stream)) {
                        dataToLoad = reader.ReadToEnd();
                    }
                }
                //if (encryptData)
                //    dataToLoad = EncryptDecrypt(dataToLoad);
                //JsonConvert.PopulateObject(dataToLoad, loadData);
                byte[] compressed = File.ReadAllBytes(fullPath);
                
                byte[] bytes = Decompress(compressed);

                loadData = MessagePackSerializer.Deserialize<MapData>(bytes);
            } catch (Exception e) {
                Debug.Log("尝试加载数据文件路径错误：" + fullPath + "\n" + e);
            }
        }
        Debug.Log("结束加载：" + DateTime.Now);
        return loadData;
    }
    public void Delete() {
        string fullPath = Path.Combine(dataDirPath, dataFileName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

    }

    //加解密
    private string EncryptDecrypt(string data) {
        // j ^ R = 1
        // 1 ^ R = j

        string modifiendData = "";

        for (int i = 0; i < data.Length; i++) {
            modifiendData += (char)(data[i] ^ codeWord[i % codeWord.Length]);
        }

        return modifiendData;
    }

    //压缩
    byte[] Compress(byte[] input) {
        //return LZ4Bytes.Compress(input, LZ4Level.L12_MAX);
        return input;
    }

    byte[] Decompress(byte[] input) {
        //return LZ4Bytes.Decompress(input);
        return input;
    }
}
