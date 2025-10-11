using Newtonsoft.Json;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;

namespace Utils.Serialization
{
    public class BitmapFileJsonConverter : JsonConverter<Bitmap>
    {
        public static string ImageDirectoryPath { get; set; } = string.Empty;

        public override Bitmap ReadJson(JsonReader reader, Type objectType, Bitmap existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            if (reader.TokenType != JsonToken.String)
            {
                throw new JsonSerializationException($"BitmapFileJsonConverter: Unexpected token '{reader.TokenType}'. File name string required.");
            }
            var fileName = (string)reader.Value;
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }
            try
            {
                var imagePath = Path.Combine(ImageDirectoryPath, fileName);
                if (File.Exists(imagePath) == false)
                {
                    throw new FileNotFoundException($"Bitmap file not found: {imagePath}");
                }

                using (var fileStream = File.OpenRead(imagePath))
                {
                    using (var sourceBitmap = new Bitmap(fileStream))
                    {
                        return new Bitmap(sourceBitmap);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public override void WriteJson(JsonWriter writer, Bitmap value, JsonSerializer serializer)
        {
            if (string.IsNullOrWhiteSpace(ImageDirectoryPath))
            {
                throw new InvalidOperationException("BitmapFileJsonConverter.ImageDirectoryPath is not set.");
            }
            Directory.CreateDirectory(ImageDirectoryPath);
            try
            {
                byte[] pngBytes;
                using (var memoryStream = new MemoryStream())
                {
                    value.Save(memoryStream, ImageFormat.Png);
                    pngBytes = memoryStream.ToArray();
                }

                string fileHash;
                using (var sha256 = SHA256.Create())
                {
                    var hashBytes = sha256.ComputeHash(pngBytes);
                    fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }

                var fileName = $"img-{fileHash}.png";
                var filePath = Path.Combine(ImageDirectoryPath, fileName);
                if (!File.Exists(filePath))
                {
                    File.WriteAllBytes(filePath, pngBytes);
                }
                writer.WriteValue(fileName);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
