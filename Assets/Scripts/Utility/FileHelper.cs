using UnityEngine;
using System.IO;
using System.Text;
using System.Collections;

namespace Kenaz
{
    static public class FileHelper
    {
        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target, bool overwrite = false)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
            {
                var path = Path.Combine(target.FullName, file.Name);
                if (overwrite)
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                file.CopyTo(path);
            }
        }

        static public bool CheckFileExist(string filePath, string fileName)
        {
            return File.Exists(Path.Combine(filePath, fileName));
        }

        static public bool CheckFileExist(string fileName)
        {
            return File.Exists(Path.Combine(Application.persistentDataPath, fileName));
        }

        static public void CreateDirectory(string folderPath)
        {
            if (Directory.Exists(folderPath) == false) { Directory.CreateDirectory(folderPath); }
        }

        static public void WriteData(string filePath, string fileName, string data)
        {
            DeleteFile(filePath, fileName);

            CreateFile(filePath, fileName, data);
        }

        static public void WriteData(string fileName, string data)
        {
            WriteData(Application.persistentDataPath, fileName, data);
        }

        static public void SaveFile(string filePath, string fileName, byte[] bytes)
        {
            FileInfo fi = new FileInfo(Path.Combine(filePath, fileName));
            string folderPath = fi.DirectoryName;
            if (Directory.Exists(folderPath) == false) { Directory.CreateDirectory(folderPath); }
            if (fi.Exists) { fi.IsReadOnly = false; }
            File.WriteAllBytes(fi.FullName, bytes);
        }

        static public void SaveFile(string fileName, byte[] bytes)
        {
            SaveFile(Application.persistentDataPath, fileName, bytes);
        }

        static public void LoadImage(string fileName, System.Action<Sprite> action)
        {
#if UNITY_STANDALONE_OSX
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
#else
            string filePath = Application.persistentDataPath + "/" + fileName;
#endif
            if (File.Exists(filePath))
            {
                StartLoadSprite(filePath, action);
            }
            else
            {
                action(null);
            }
        }

        static void StartLoadSprite(string filePath, System.Action<Sprite> action)
        {
            byte[] data = File.ReadAllBytes(filePath);
            action(StartLoadSpriteByBytes(data, filePath));
        }

        public static Sprite StartLoadSpriteByBytes(byte[] bytes, string fileName)
        {
            // Create a texture. Texture size does not matter, since
            // LoadImage will replace with with incoming image size.
            // https://docs.unity3d.com/ScriptReference/ImageConversion.LoadImage.html
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(bytes);
            texture.name = Path.GetFileNameWithoutExtension(fileName);
            //texture.Compress(false);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        static public void CreateFile(string filePath, string fileName, string data)
        {
            //文件流信息.
            StreamWriter sw;
            FileInfo t = new FileInfo(filePath + "//" + fileName);
            if (!t.Exists)
            {
                sw = t.CreateText();
            }
            else
            {
                sw = t.AppendText();
            }
            sw.WriteLine(data);
            sw.Close();
            sw.Dispose();
        }

        static public string LoadFile(string fileName)
        {
            return LoadFile(Application.persistentDataPath, fileName);
        }

        static public string LoadFileFullPath(string fileFullPath)
        {
            StreamReader sr = null;
            try
            {
                sr = File.OpenText(fileFullPath);
            }
            catch (System.Exception e)
            {

                Debug.Log(e.Message);
                return null;
            }
            string line;
            line = sr.ReadToEnd();
            sr.Close();
            sr.Dispose();
            return line;
        }

        static public string LoadFile(string filePath, string fileName)
        {
            return LoadFileFullPath(filePath + "//" + fileName);
        }

        static public void DeleteFile(string filePath, string fileName)
        {
            if (File.Exists(filePath + "//" + fileName))
            {
                File.Delete(filePath + "//" + fileName);
            }
        }
    }

}