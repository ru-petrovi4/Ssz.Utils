using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Core.ControlsCommon.Trends
{
    public class DateRange
    {
        public DateRange(DateTime minimum, DateTime maximum)
        {
            Minimum = minimum <= maximum ? minimum : maximum;
            Maximum = minimum <= maximum ? maximum : minimum;
        }

        // [minimum ..... timePoint ..... maximum]
        //           at timePointPercentage (0 = minimum, 1 = maximum)
        public DateRange(DateTime timePoint, TimeSpan totalRange, double timePointPercentage = 0.5)
        {
            Minimum = timePoint - TimeSpan.FromTicks((long)(totalRange.Ticks * timePointPercentage));
            Maximum = Minimum + totalRange;
        }

        public DateTime Minimum { get; private set; }
        public DateTime Maximum { get; private set; }

        public TimeSpan Range
        {
            get { return Maximum - Minimum; }
        }

        public DateTime Center
        {
            get { return Interpolate(0.5); }
        }

        public DateTime Interpolate(double coefficientFrom0To1)
        {
            return Minimum + new TimeSpan((long)(Range.Ticks * coefficientFrom0To1));
        }

        public DateRange Pan(TimeSpan delta)
        {
            return new DateRange(Minimum + delta, Maximum + delta);
        }

        // [min + p%, max + p%]
        public DateRange Pan(double percentage)
        {
            return Pan(TimeSpan.FromTicks((long)(Range.Ticks * percentage)));
        }

        public DateRange AddPadding(TimeSpan range)
        {
            return new DateRange(Minimum - range, Maximum + range);
        }

        public double Percentage(DateTime date)
        {
            return (double)(date - Minimum).Ticks / (Maximum - Minimum).Ticks;
        }

        public bool Includes(DateTime oldNow)
        {
            return Minimum <= oldNow && oldNow <= Maximum;
        }

        // makes range to be included in [min, max]
        public DateRange Clamp(DateRange range)
        {
            return new DateRange(
                range.Minimum > Minimum ? range.Minimum : Minimum,
                range.Maximum < Maximum ? range.Maximum : Maximum);
        }

        public override bool Equals(object? obj)
        {
            var that = obj as DateRange;
            if (that == null)
                return false;

            return Minimum == that.Minimum && Maximum == that.Maximum;
        }

        public override int GetHashCode()
        {
            return Minimum.GetHashCode() ^ Maximum.GetHashCode();
        }

        public override string ToString()
        {
            return Minimum.Date == Maximum.Date
                ? string.Format("[{0} - {1}] ({2})",
                    Minimum.TimeOfDay,
                    Maximum.TimeOfDay,
                    Minimum.Date.ToShortDateString())
                : string.Format("[{0} - {1}]", Minimum, Maximum);
        }
    }
}
