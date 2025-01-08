namespace DataContainer.Generated
{
    public class TemplateBase
    {
        public const int InvidateTemplateId = -1;
        public int Id { get; set; } = InvidateTemplateId;
        public string Name { get; set; }
        public bool Invalid()
        {
            return Id == InvidateTemplateId;
        }
        public virtual void Combine()
        {
        }
        public virtual void MakeRefTemplate()
        {
        }
    }
}