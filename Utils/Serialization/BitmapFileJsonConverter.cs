using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Utils.Serialization
{
    public class BitmapFileJsonConverter : JsonConverter<Bitmap>
    {
        public static string ImageDirectoryPath { get; set; } = string.Empty;

        public override Bitmap Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Bitmap file name must be a string.");
            }
            string fileName = reader.GetString();
            if (string.IsNullOrWhiteSpace(fileName) == true)
            {
                return null;
            }
            string imagePath = Path.Combine(ImageDirectoryPath, fileName);
            using FileStream fileStream = File.OpenRead(imagePath);
            using Bitmap sourceBitmap = new(fileStream);
            return new Bitmap(sourceBitmap);
        }

        public override void Write(Utf8JsonWriter writer, Bitmap value, JsonSerializerOptions options)
        {
            if (string.IsNullOrWhiteSpace(ImageDirectoryPath) == true)
            {
                throw new InvalidOperationException("Bitmap image directory is not configured.");
            }
            Directory.CreateDirectory(ImageDirectoryPath);
            using MemoryStream memoryStream = new();
            value.Save(memoryStream, ImageFormat.Png);
            byte[] pngBytes = memoryStream.ToArray();
            string fileHash = Convert.ToHexString(SHA256.HashData(pngBytes)).ToLowerInvariant();
            string fileName = $"img-{fileHash}.png";
            string filePath = Path.Combine(ImageDirectoryPath, fileName);
            if (File.Exists(filePath) == false)
            {
                File.WriteAllBytes(filePath, pngBytes);
            }
            writer.WriteStringValue(fileName);
        }
    }
}
