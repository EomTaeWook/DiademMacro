using Dignus.Log;
using Dignus.Utils.Extensions;
using Macro.Infrastructure.Serialize;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Utils;

namespace Macro.Infrastructure.Manager
{
    public class FileService
    {
        public bool SaveJson<T>(string path, T model)
        {
            try
            {
                var json = JsonHelper.SerializeObject(model, true);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message);

                return false;
            }

            return true;
        }

        public List<T> Load<T>(string path)
        {
            if(File.Exists(path) == false)
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
                    while ((read = stream.ReadAsync(buffer, 0, (int)stream.Length).GetResult()) != 0)
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

        public void Save<T>(string path, ObservableCollection<T> list)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (var fs = new FileStream(path, FileMode.OpenOrCreate))
            {
                foreach (var item in list)
                {
                    var bytes = ObjectSerializer.SerializeObject(item);
                    fs.WriteAsync(bytes, 0, bytes.Count()).GetResult();
                }
                fs.Close();
            }
        }
        public void Save<T>(string path, List<T> list)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (var fs = new FileStream(path, FileMode.OpenOrCreate))
            {
                foreach (var item in list)
                {
                    var bytes = ObjectSerializer.SerializeObject(item);
                    fs.WriteAsync(bytes, 0, bytes.Count()).GetResult();
                }
                fs.Close();
            }
        }
    }
}