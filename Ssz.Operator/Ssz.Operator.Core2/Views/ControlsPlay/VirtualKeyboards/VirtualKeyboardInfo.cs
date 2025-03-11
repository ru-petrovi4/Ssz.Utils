namespace Ssz.Operator.Core.ControlsPlay.VirtualKeyboards
{
    public class VirtualKeyboardInfo
    {
        public VirtualKeyboardInfo(string type, string nameToDisplay, string description)
        {
            Type = type;
            NameToDisplay = nameToDisplay;
            Description = description;
        }


        public string Type { get; set; }


        public string NameToDisplay { get; set; }


        public string Description { get; set; }
    }
}