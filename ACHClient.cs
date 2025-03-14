using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace WsdlExMachina.Generated
{
    [DataContract(Namespace = "http://www.swbc.com/")]
    public class ACHTransResponse
    {
        [DataMember(Name = "ResponseCode", IsRequired = false)]
        public string ResponseCode { get; set; }

        [DataMember(Name = "ResponseMessage", IsRequired = false)]
        public string ResponseMessage { get; set; }

        [DataMember(Name = "ResponseStringRaw", IsRequired = false)]
        public string ResponseStringRaw { get; set; }
    }

    [DataContract(Namespace = "http://www.swbc.com/")]
    public class SWBCAuthHeader
    {
        [DataMember(Name = "Username", IsRequired = false)]
        public string Username { get; set; }

        [DataMember(Name = "Password", IsRequired = false)]
        public string Password { get; set; }
    }

    [DataContract(Namespace = "http://www.swbc.com/")]
    public class ArrayOfApplyFundsTo
    {
        [DataMember(Name = "ApplyFundsTo", IsRequired = false)]
        public List<ApplyFundsTo> ApplyFundsTo { get; set; } = new List<ApplyFundsTo>();
    }

    [DataContract(Namespace = "http://www.swbc.com/")]
    public class ApplyFundsTo
    {
        [DataMember(Name = "ApplyToAccountNumber", IsRequired = false)]
        public string ApplyToAccountNumber { get; set; }

        [DataMember(Name = "ApplyToName", IsRequired = false)]
        public string ApplyToName { get; set; }

        [DataMember(Name = "ApplyToAccountType", IsRequired = true)]
        public char ApplyToAccountType { get; set; }

        [DataMember(Name = "ApplyToAmount", IsRequired = true)]
        public double ApplyToAmount { get; set; }
    }

    [DataContract(Namespace = "http://www.swbc.com/")]
    public class ArrayOfString
    {
        [DataMember(Name = "string", IsRequired = false)]
        public List<string> String { get; set; } = new List<string>();
    }

    [DataContract(Namespace = "http://www.swbc.com/")]
    public enum PaymentSource
    {
        [EnumMember(Value = "OnlineAkcelerant")]
        OnlineAkcelerant,
        [EnumMember(Value = "OnlineECM")]
        OnlineECM,
        [EnumMember(Value = "OnlineWeblet")]
        OnlineWeblet,
        [EnumMember(Value = "OnlineModules")]
        OnlineModules,
        [EnumMember(Value = "ScheduledTask_StatusResubmission")]
        ScheduledTaskStatusResubmission,
        [EnumMember(Value = "ScheduledTask_SubmitRecurringTransactions")]
        ScheduledTaskSubmitRecurringTransactions,
        [EnumMember(Value = "ScheduledTask_GeneratePayments")]
        ScheduledTaskGeneratePayments,
        [EnumMember(Value = "WebService_AKCPaymentProcessor")]
        WebServiceAKCPaymentProcessor,
        [EnumMember(Value = "WebService_IVRPaymentProcessor")]
        WebServiceIVRPaymentProcessor,
        [EnumMember(Value = "WebService_PaymentProcessor")]
        WebServicePaymentProcessor,
        [EnumMember(Value = "WebService_Cash")]
        WebServiceCash,
        [EnumMember(Value = "BaconLoanPay")]
        BaconLoanPay,
        [EnumMember(Value = "BaconEnterprise")]
        BaconEnterprise,
        [EnumMember(Value = "Payments")]
        Payments,
        [EnumMember(Value = "Symitar")]
        Symitar,
        [EnumMember(Value = "MeridianLink")]
        MeridianLink,
        [EnumMember(Value = "SymXchange")]
        SymXchange,
        [EnumMember(Value = "Akuvo")]
        Akuvo,
        [EnumMember(Value = "ClarkCountySaml")]
        ClarkCountySaml,
        [EnumMember(Value = "Tccs")]
        Tccs,
        [EnumMember(Value = "CovantageSaml")]
        CovantageSaml,
        [EnumMember(Value = "IvrSupportTeam")]
        IvrSupportTeam
    }

    [ServiceContract(Namespace = "http://www.swbc.com/")]
    public interface IACHTransactionSoap
    {
        [OperationContract(Action = "http://www.swbc.com/PostSinglePayment")]
        Task<> PostSinglePayment(parameters)[OperationContract(Action = "http://www.swbc.com/PostSinglePaymentV3_1")]
        Task<> PostSinglePaymentV3_1(parameters)[OperationContract(Action = "http://www.swbc.com/PostSinglePayment")]
        Task<> PostSinglePayment(parameters)[OperationContract(Action = "http://www.swbc.com/PostSinglePaymentV3_1")]
        Task<> PostSinglePaymentV3_1(parameters)[OperationContract(Action = "http://www.swbc.com/PostSinglePayment")]
        Task<> PostSinglePayment(parameters)[OperationContract(Action = "http://www.swbc.com/PostSinglePaymentV3_1")]
        Task<> PostSinglePaymentV3_1(parameters)[OperationContract(Action = "http://www.swbc.com/PostSinglePayment")]
        Task<> PostSinglePayment(parameters)[OperationContract(Action = "http://www.swbc.com/PostSinglePaymentV3_1")]
        Task<> PostSinglePaymentV3_1(parameters)[OperationContract(Action = "http://www.swbc.com/PostSinglePayment")]
        Task<> PostSinglePayment(parameters)[OperationContract(Action = "http://www.swbc.com/PostSinglePaymentV3_1")]
        Task<> PostSinglePaymentV3_1(parameters)[OperationContract(Action = "http://www.swbc.com/PostACHPaymentWithApplyFundsToList")]
        Task<> PostACHPaymentWithApplyFundsToList(parameters)[OperationContract(Action = "http://www.swbc.com/PostSinglePaymentWithACHWebValidation")]
        Task<> PostSinglePaymentWithAchWebValidation(parameters)[OperationContract(Action = "http://www.swbc.com/PostSinglePaymentWithACHWebValidation")]
        Task<> PostSinglePaymentWithAchWebValidation(parameters)[OperationContract(Action = "http://www.swbc.com/PostSinglePayment")]
        Task<> PostSinglePayment(parameters)[OperationContract(Action = "http://www.swbc.com/PostSinglePaymentV3_1")]
        Task<> PostSinglePaymentV3_1(parameters)[OperationContract(Action = "http://www.swbc.com/PostSinglePaymentV3_1")]
        Task<> PostSinglePaymentV3_1(parameters)[OperationContract(Action = "http://www.swbc.com/PostRecurringPayment")]
        Task<> PostRecurringPayment(parameters)[OperationContract(Action = "http://www.swbc.com/PostRecurringPaymentV3_1")]
        Task<> PostRecurringPaymentV3_1(parameters)[OperationContract(Action = "http://www.swbc.com/ValidateABANumber")]
        Task<> ValidateABANumber(parameters)[OperationContract(Action = "http://www.swbc.com/GetABACompletionList")]
        Task<> GetABACompletionList(parameters)[OperationContract(Action = "http://www.swbc.com/GetStatusUpdate")]
        Task<> GetStatusUpdate(parameters)[OperationContract(Action = "http://www.swbc.com/GetStatusUpdateForList")]
        Task<> GetStatusUpdateForList(parameters)[OperationContract(Action = "http://www.swbc.com/TestF5Function")]
        Task<> TestF5Function(parameters)}

    public class ACHTransactionClient : ClientBase<IACHTransactionSoap>, IACHTransactionSoap
    {
        public ACHTransactionClient()
        {
        }

        public ACHTransactionClient(string endpointConfigurationName) : base(endpointConfigurationName)
        {
        }

        public ACHTransactionClient(string endpointConfigurationName, string remoteAddress) : base(endpointConfigurationName, remoteAddress)
        {
        }

        public ACHTransactionClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : base(binding, remoteAddress)
        {
        }

        public Task<> PostSinglePayment(parameters)
        {
            return this.Channel.PostSinglePayment(parameters);
        }

        public Task<> PostSinglePaymentV3_1(parameters)
        {
            return this.Channel.PostSinglePaymentV3_1(parameters);
        }

        public Task<> PostSinglePayment(parameters)
        {
            return this.Channel.PostSinglePayment(parameters);
        }

        public Task<> PostSinglePaymentV3_1(parameters)
        {
            return this.Channel.PostSinglePaymentV3_1(parameters);
        }

        public Task<> PostSinglePayment(parameters)
        {
            return this.Channel.PostSinglePayment(parameters);
        }

        public Task<> PostSinglePaymentV3_1(parameters)
        {
            return this.Channel.PostSinglePaymentV3_1(parameters);
        }

        public Task<> PostSinglePayment(parameters)
        {
            return this.Channel.PostSinglePayment(parameters);
        }

        public Task<> PostSinglePaymentV3_1(parameters)
        {
            return this.Channel.PostSinglePaymentV3_1(parameters);
        }

        public Task<> PostSinglePayment(parameters)
        {
            return this.Channel.PostSinglePayment(parameters);
        }

        public Task<> PostSinglePaymentV3_1(parameters)
        {
            return this.Channel.PostSinglePaymentV3_1(parameters);
        }

        public Task<> PostACHPaymentWithApplyFundsToList(parameters)
        {
            return this.Channel.PostACHPaymentWithApplyFundsToList(parameters);
        }

        public Task<> PostSinglePaymentWithAchWebValidation(parameters)
        {
            return this.Channel.PostSinglePaymentWithAchWebValidation(parameters);
        }

        public Task<> PostSinglePaymentWithAchWebValidation(parameters)
        {
            return this.Channel.PostSinglePaymentWithAchWebValidation(parameters);
        }

        public Task<> PostSinglePayment(parameters)
        {
            return this.Channel.PostSinglePayment(parameters);
        }

        public Task<> PostSinglePaymentV3_1(parameters)
        {
            return this.Channel.PostSinglePaymentV3_1(parameters);
        }

        public Task<> PostSinglePaymentV3_1(parameters)
        {
            return this.Channel.PostSinglePaymentV3_1(parameters);
        }

        public Task<> PostRecurringPayment(parameters)
        {
            return this.Channel.PostRecurringPayment(parameters);
        }

        public Task<> PostRecurringPaymentV3_1(parameters)
        {
            return this.Channel.PostRecurringPaymentV3_1(parameters);
        }

        public Task<> ValidateABANumber(parameters)
        {
            return this.Channel.ValidateABANumber(parameters);
        }

        public Task<> GetABACompletionList(parameters)
        {
            return this.Channel.GetABACompletionList(parameters);
        }

        public Task<> GetStatusUpdate(parameters)
        {
            return this.Channel.GetStatusUpdate(parameters);
        }

        public Task<> GetStatusUpdateForList(parameters)
        {
            return this.Channel.GetStatusUpdateForList(parameters);
        }

        public Task<> TestF5Function(parameters)
        {
            return this.Channel.TestF5Function(parameters);
        }
    }
}