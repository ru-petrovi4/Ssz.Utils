using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
    public interface IProgressInfo
    {
        int ProgressBarCurrentValue { get; set; }


        int ProgressBarMaxValue { get; set; }


        Task SetHeaderAsync(string text);


        Task SetDescriptionAsync(string text);


        Task RefreshProgressBarAsync();


        Task RefreshProgressBarAsync(int currentValue, int maxValue);


        Task DebugInfoAsync(string info);


        CancellationToken GetCancellationToken();
    }
}