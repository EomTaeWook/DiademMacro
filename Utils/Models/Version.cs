namespace Utils.Models
{
    public class Version
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Build { get; set; }
        public Version()
        {
        }
        public Version(int major, int minor, int build)
        {
            Major = major;
            Minor = minor;
            Build = build;
        }

        public string ToVersionString()
        {
            return $"{Major}.{Minor}.{Build}";
        }
    }
}
