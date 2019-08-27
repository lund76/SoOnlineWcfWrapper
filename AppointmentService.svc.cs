using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using SuperOffice;
using SuperOffice.Configuration;
using SuperOffice.CRM.Services;
using SuperOffice.Security.Principal;
using SuperOffice.SuperID.Client.Tokens;

namespace SoOnlineWcfWrapper
{
    public class AppointmentService : IAppointmentService
    {
        public string GetUserTicket(string userToken, string contextIdentifier,
            string privateKey, string federationGateway, string applicationToken, string certificateString)
        {

            var soToken = GetSystemUserToken(userToken, contextIdentifier,
                privateKey, federationGateway, applicationToken, certificateString);
            return soToken.Ticket;
        }

        public MiniToken GetMiniIdToken(string userToken, string contextIdentifier, string privateKey,
            string federationGateway,
            string applicationToken, string certificateString)
        {
            var soToken = GetSystemUserToken(userToken, contextIdentifier,
                privateKey, federationGateway, applicationToken, certificateString);

            //Serilizable ticket
            return new MiniToken
            {
                ContextIdentifier = soToken.ContextIdentifier, NetserverUrl = soToken.NetserverUrl,
                Token = soToken.Ticket
            };
        }

        public IEnumerable<AppointmentInfo> GetAppointmentEntitiesFromId(int appointmentId, string ticket,
        string contextIdentifier, string netserverUrl, Dictionary<string,WishlistElement> infoWishList)
        {
            using (SoDatabaseContext.EnterDatabaseContext(contextIdentifier))
            {
                ConfigFile.WebServices.RemoteBaseURL = netserverUrl;
                using (SoSession.Authenticate(new SoCredentials(ticket)))
                using (var appAgent = new AppointmentAgent())
                //using (new AssociateAgent())
                using (var personAgent = new PersonAgent())
                {
               
                    var appEnts = new List<AppointmentInfo>();
                    AppointmentInfo appInfo;
                    AppointmentEntity appEntity;
                    do
                    {
                        appEntity = appAgent.GetAppointmentEntity(appointmentId);
                        if ( appEntity?.Associate == null) continue;
            
                        var associatePerson = personAgent.GetPersonEntity(appEntity.Associate.PersonId);
                        var appointmentPerson = personAgent.GetPersonEntity(appEntity.Person.PersonId);

                        appInfo = new AppointmentInfo
                        {
                            AppointmentId = appEntity.AppointmentId,
                            EmailReceiver = appointmentPerson.Emails.FirstOrDefault()?.Value,
                            MessageDescription = appEntity.Description,
                            Receptionist = associatePerson.FullName,
                            RecepTitle = associatePerson.Title,
                            SmsPhoneNumber = appointmentPerson.MobilePhones.FirstOrDefault()?.Value,
                            CustWantsCallBack = ResolveVariableInfo(appEntity,
                                infoWishList.FirstOrDefault(k => k.Key == "CustWantsCallBack").Value),
                            SendSms = ResolveVariableInfo(appointmentPerson,
                                infoWishList.FirstOrDefault(k => k.Key == "SendSms").Value),
                            CustCallsBack = ResolveVariableInfo(appEntity,
                                infoWishList.FirstOrDefault(k => k.Key == "CustCallsBack").Value),
                            CustomerHighPriority = ResolveVariableInfo(appEntity,
                                infoWishList.FirstOrDefault(k => k.Key == "CustomerHighPriority").Value),
                            Customer = ResolveVariableInfo(appEntity,
                                infoWishList.FirstOrDefault(k => k.Key == "Customer").Value),
                            CustomerContact = ResolveVariableInfo(appEntity,
                                infoWishList.FirstOrDefault(k => k.Key == "CustomerContact").Value),
                            CustomerContactPhone =ResolveVariableInfo(appEntity,
                                infoWishList.FirstOrDefault(k => k.Key == "CustomerContactPhone").Value),

                        };

                        appEnts.Add(appInfo);
                        appointmentId++;
                        Debug.WriteLine($"AppEntity desc {appInfo.MessageDescription} with ID {appointmentId}");

                    } while (appEntity != null);

                    return appEnts;
                }
            }
        }

        private string ResolveVariableInfo(object entity , WishlistElement val)
        {

            if (val == null) return null;
            var result = "";
            
            
                switch (val.ApplicationContext.ToUpper())
                {
                    case "PERSONINTEREST":
                        if(!(entity is PersonEntity pEnt))
                            throw new Exception("Entity type not correctly resolved");
                        var i = pEnt.Interests.FirstOrDefault(n => n.Name.Contains(val.FieldKey));
                        result = i == null ? "False" : "True";
                        break;
                    case "APPOINTMENTUDEF":
                        if(!(entity is AppointmentEntity appEnt))
                            throw new Exception("Entity type not correctly resolved");
                        var a = appEnt.UserDefinedFields.FirstOrDefault(k => k.Key == val.FieldKey);
                        result = a.Value;
                      break;
                }
            return result;
        }


        public static SuperIdToken GetSystemUserToken(string userToken, string contextIdentifier,
            string privateKey, string federationGateway, string applicationToken, string certificateString)
        {
            var tokenType = SuperOffice.SuperID.Contracts.SystemUser.V1.TokenType.Jwt;

            var systemToken = new SystemToken(userToken);

            // Get certificate
          
            // sign the system user ticket
            var signedSystemToken = systemToken.Sign(privateKey);

            // Call the web service to exchange signed system user ticket with claims for the system user
            var returnedToken = systemToken.AuthenticateWithSignedSystemToken(federationGateway, signedSystemToken,
                applicationToken, contextIdentifier, tokenType);

            if (returnedToken != null)
            {
               
                // Validate and return SuperId ticket for the system user
                var tokenHandler = new SuperIdTokenHandler();

                var certificateResolverPath = AppDomain.CurrentDomain.BaseDirectory + "Certificates";

                if (tokenType == SuperOffice.SuperID.Contracts.SystemUser.V1.TokenType.Saml)
                {
                    tokenHandler.CertificateValidator = System.IdentityModel.Selectors.X509CertificateValidator.None;
                    tokenHandler.IssuerTokenResolver = new CertificateFileCertificateStoreTokenResolver(certificateResolverPath);
                }
                else
                {
                    // byte[] bytes = System.Convert.FromBase64String(certificateString);
                    byte[] bytes = Encoding.ASCII.GetBytes(certificateString);
                    tokenHandler.JwtIssuerSigningCertificate =
                        new System.Security.Cryptography.X509Certificates.X509Certificate2(bytes);
                }

                tokenHandler.ValidateAudience = false;

                SuperIdToken superToken = null;

                try
                {
                    superToken = tokenHandler.ValidateToken(returnedToken, tokenType);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                return superToken; 
            }

            return null;
        }
    }


  

    [DataContract]
    public class AppointmentInfo : EdelmannPhoneMessage.Core.IAppointmentInfo
    {
        [DataMember]
        public int AppointmentId { get; set; }
        [DataMember]
        public string MessageDescription { get; set; }
        [DataMember]
        public string EmailReceiver { get; set; }
        [DataMember]
        public string SendSms { get; set; }
        [DataMember]
        public string Customer { get; set; }
        [DataMember]
        public string CustomerContact { get; set; }
        [DataMember]
        public string CustomerContactPhone { get; set; }
        [DataMember]
        public string CustCallsBack { get; set; }
        [DataMember]
        public string CustomerHighPriority { get; set; }
        [DataMember]
        public string Receptionist { get; set; }
        [DataMember]
        public string RecepTitle  { get; set; }
        [DataMember]
        public string CustWantsCallBack { get; set; }
        [DataMember]
        public string SmsPhoneNumber { get; set; }
    }
   
}
