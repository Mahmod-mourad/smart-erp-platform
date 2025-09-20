using Microsoft.ML.Data;

namespace NexaFlow.Infrastructure.ML;

// ML.NET input/output schema types. They carry Microsoft.ML column attributes, so they live
// in Infrastructure (the Application layer must stay free of ML.NET). The public result
// shapes returned to callers are the pure DTOs in NexaFlow.Application.DTOs.

/// <summary>One point in the monthly sales time-series fed to the SSA forecaster.</summary>
public sealed class SalesDataPoint
{
    public float Value { get; set; }
}

/// <summary>SSA forecaster output: the prediction plus its confidence band.</summary>
public sealed class SalesForecastOutput
{
    public float[] ForecastedValues { get; set; } = Array.Empty<float>();
    public float[] ConfidenceLowerBound { get; set; } = Array.Empty<float>();
    public float[] ConfidenceUpperBound { get; set; } = Array.Empty<float>();
}

/// <summary>Per-customer feature vector used to train/score the churn classifier.</summary>
public sealed class CustomerFeatures
{
    public float DaysSinceLastPurchase { get; set; }
    public float TotalPurchases { get; set; }
    public float AveragePurchaseValue { get; set; }
    public float TotalSpent { get; set; }
    public float MonthsAsCustomer { get; set; }

    /// <summary>Training label: true = the customer has churned.</summary>
    [ColumnName("Label")]
    public bool HasChurned { get; set; }
}

/// <summary>Churn classifier prediction for a single customer.</summary>
public sealed class ChurnPrediction
{
    [ColumnName("PredictedLabel")]
    public bool WillChurn { get; set; }

    public float Probability { get; set; }

    public float Score { get; set; }
}
