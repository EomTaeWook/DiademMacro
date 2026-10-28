using System;
using System.Globalization;

namespace Macro.Infrastructure.Models.Updates
{
    public class ApplicationReleaseVersion : IComparable<ApplicationReleaseVersion>
    {
        private ApplicationReleaseVersion(int major, int minor, int patch, string prerelease, string text)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Prerelease = prerelease;
            Text = text;
        }

        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public string Prerelease { get; }
        public string Text { get; }
        public bool IsStable
        {
            get
            {
                return Prerelease.Length == 0;
            }
        }

        public static bool TryParse(string text, out ApplicationReleaseVersion version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(text) == true)
            {
                return false;
            }
            string value = text.Trim();
            if (value.StartsWith("v", StringComparison.Ordinal) == true)
            {
                value = value.Substring(1);
            }
            int metadata = value.IndexOf('+');
            if (metadata >= 0)
            {
                value = value.Substring(0, metadata);
            }
            string prerelease = string.Empty;
            string numbers = value;
            int separator = value.IndexOf('-');
            if (separator >= 0)
            {
                prerelease = value.Substring(separator + 1);
                if (prerelease.Length == 0)
                {
                    return false;
                }
                foreach (char character in prerelease)
                {
                    if (char.IsAsciiLetterOrDigit(character) == true)
                    {
                        continue;
                    }
                    if (character == '-')
                    {
                        continue;
                    }
                    if (character == '.')
                    {
                        continue;
                    }
                    return false;
                }
                numbers = value.Substring(0, separator);
            }
            string[] parts = numbers.Split('.');
            if (parts.Length != 3)
            {
                return false;
            }
            int[] parsed = new int[3];
            for (int index = 0; index < parts.Length; index++)
            {
                if (parts[index].Length == 0)
                {
                    return false;
                }
                if (parts[index].Length > 1)
                {
                    if (parts[index][0] == '0')
                    {
                        return false;
                    }
                }
                if (int.TryParse(parts[index], NumberStyles.None, CultureInfo.InvariantCulture, out parsed[index]) == false)
                {
                    return false;
                }
            }
            version = new ApplicationReleaseVersion(parsed[0], parsed[1], parsed[2], prerelease, value);
            return true;
        }

        public int CompareTo(ApplicationReleaseVersion other)
        {
            if (other == null)
            {
                return 1;
            }
            int comparison = Major.CompareTo(other.Major);
            if (comparison != 0)
            {
                return comparison;
            }
            comparison = Minor.CompareTo(other.Minor);
            if (comparison != 0)
            {
                return comparison;
            }
            comparison = Patch.CompareTo(other.Patch);
            if (comparison != 0)
            {
                return comparison;
            }
            if (IsStable == other.IsStable)
            {
                return 0;
            }
            if (IsStable == true)
            {
                return 1;
            }
            return -1;
        }
    }
}
