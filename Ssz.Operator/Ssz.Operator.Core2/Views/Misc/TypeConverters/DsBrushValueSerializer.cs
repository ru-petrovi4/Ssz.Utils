using Avalonia.Markup;


namespace Ssz.Operator.Core
{
    //public class DsBrushValueSerializer // : ValueSerializer
    //{
    //    #region public functions

    //    public static readonly DsBrushValueSerializer Instance = new();

    //    public override bool CanConvertToString(object value, IValueSerializerContext? context)
    //    {
    //        if (value is SolidDsBrush)
    //            return true;
    //        if (value is BlinkingDsBrush)
    //            return true;
    //        return false;
    //    }

    //    public override string? ConvertToString(object value, IValueSerializerContext? context)
    //    {
    //        var solidDsBrush = value as SolidDsBrush;
    //        if (solidDsBrush is not null) return solidDsBrush.ColorString;
    //        var blinkingDsBrush = value as BlinkingDsBrush;
    //        if (blinkingDsBrush is not null)
    //            return blinkingDsBrush.FirstColorString + ";" + blinkingDsBrush.SecondColorString;
    //        return null;
    //    }

    //    #endregion
    //}
}