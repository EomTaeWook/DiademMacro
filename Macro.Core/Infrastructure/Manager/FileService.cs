using Dignus.Log;
using Macro.Infrastructure.Serialize;
using Dignus.DependencyInjection.Attributes;
using Macro.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utils;

namespace Macro.Infrastructure.Manager
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    public class FileService
    {
        public bool SaveJson<T>(string path, T model)
        {
            return SaveSerializedJson(path, model);
        }

        private bool SaveSerializedJson<T>(string path, T model)
        {
            string temporaryPath = null;
            try
            {
                string json = JsonHelper.SerializeObject(model, true);
                string fullPath = Path.GetFullPath(path);
                string directoryPath = Path.GetDirectoryName(fullPath);
                Directory.CreateDirectory(directoryPath);
                temporaryPath = Path.Combine(directoryPath, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
                File.WriteAllText(temporaryPath, json);
                if (File.Exists(fullPath) == true)
                {
                    File.Replace(temporaryPath, fullPath, null);
                }
                else
                {
                    File.Move(temporaryPath, fullPath);
                }
            }
            catch (Exception exception)
            {
                LogHelper.Error(exception);
                return false;
            }
            finally
            {
                if (temporaryPath != null)
                {
                    try
                    {
                        File.Delete(temporaryPath);
                    }
                    catch (Exception exception)
                    {
                        LogHelper.Error(exception);
                    }
                }
            }
            return true;
        }

        public List<T> Load<T>(string path)
        {
            if (File.Exists(path) == false)
            {
                LogHelper.Info($"not found file. path : {path}");
                return null;
            }

            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                {
                    byte[] buffer = new byte[stream.Length];
                    var datas = new List<byte>();
                    var read = 0;
                    while ((read = stream.ReadAsync(buffer, 0, (int)stream.Length).GetAwaiter().GetResult()) != 0)
                    {
                        datas.AddRange(buffer.Take(read));
                    }
                    stream.Close();
                    return ObjectSerializer.DeserializeObject<T>(datas.ToArray());
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
            return null;
        }
        public bool LooksLikeJson(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return true;
            }
            var json = File.ReadAllText(filePath);

            if (string.IsNullOrEmpty(json))
            {
                return true;
            }

            if (json[0] == '{' && json[json.Length - 1] == '}')
            {
                return true;
            }
            if (json[0] == '[' && json[json.Length - 1] == ']')
            {
                return true;
            }
            return false;
        }
        public List<EventInfoModel> LoadSaveAsJson(string path)
        {
            if (File.Exists(path) == false)
            {
                LogHelper.Info($"not found file. path : {path}");
                return null;
            }

            try
            {
                var json = File.ReadAllText(path);

                var loadSaveDatas = JsonHelper.DeserializeObject<List<EventInfoModel>>(json);

                return loadSaveDatas;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
            return null;
        }
        public bool SaveAsJson(string path, List<EventInfoModel> list)
        {
            return SaveSerializedJson(path, list);
        }
    }
}
