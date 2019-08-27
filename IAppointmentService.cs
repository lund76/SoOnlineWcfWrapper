using System;
using System.Collections.Generic;
using System.IdentityModel.Claims;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using SuperOffice.CRM.Services;
using SuperOffice.SuperID.Client.Tokens;

namespace SoOnlineWcfWrapper
{
    [ServiceContract(SessionMode = SessionMode.Allowed)]
    public interface IAppointmentService
    {
        [OperationContract]
        MiniToken GetMiniIdToken(string userToken, string contextIdentifier,
            string privateKey, string federationGateway, string applicationToken, string certificateString);

        [OperationContract]
        string GetUserTicket(string userToken, string contextIdentifier,
            string privateKey, string federationGateway, string applicationToken, string certificateString);

        [OperationContract]
        IEnumerable<AppointmentInfo> GetAppointmentEntitiesFromId(int appointmentId, string ticket,
            string contextIdentifier, string netserverUrl, Dictionary<string, WishlistElement> infoWishList);
    }

    [DataContract]
    public class WishlistElement
    {
        [DataMember]
        public string FieldKey { get; set; }
        [DataMember]
        public string ApplicationContext { get; set; }
    }
    [DataContract]
    public class MiniToken
    {
        [DataMember]
        public string Token { get; set; }
        [DataMember]
        public string ContextIdentifier { get; set; }
        [DataMember]
        public string NetserverUrl { get; set; }

    }
}
