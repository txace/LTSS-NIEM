using Application.Data.XML;
using LTSSWebService;
using System;
using System.ServiceModel.Channels;

namespace DigestPasswordNS
{
    public class RetrieveReferralInfoInspector : DigestPasswordClientMessageInspector
    {
        private string _referralID;
        private int _requestID;

        public RetrieveReferralInfoInspector(string userName, string passWord, int requestID, string referralID) : base(userName, passWord)
        {
            _requestID = requestID;
            _referralID = referralID;
        }

        public override Func<string, string> EvaluateResponseTemplate
        {
            get
            {
                return new Func<string, string>(x => x.Replace("[REFERRALID]", _referralID));
            }
        }

        public override string ResponseTemplate
        {
            get
            {
                return @"<soap:Envelope
	xmlns:ns=""http://ltss.referralmanagement.hhsc.state.tx.us/interface/1.1""
	xmlns:ns1=""http://ltss.referralmanagement.hhsc.state.tx.us/exchange/1.1""
	xmlns:ns2=""http://release.niem.gov/niem/structures/3.0/""
	xmlns:ns3=""http://ltss.core.hhsc.state.tx.us/ltsscore/1.1""
	xmlns:ns4=""http://release.niem.gov/niem/niem-core/3.0/""
	xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"">
	<soap:Header>
		[SECURITY]
	</soap:Header>
	<soap:Body>
		<ns:retrieveReferralInfoRequest>
			<ns1:RetrieveReferralInfoRequest ns2:id=""ID1"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">
				<ns3:TransferHeader ns2:id=""ID2"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">
					<ns3:TransferActivity ns2:id=""ID3"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">
						<!--Optional:-->
						<ns4:ActivityIdentification ns2:id=""ID4"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">
							<!--Optional:-->
							<ns4:IdentificationID ns2:id=""ID5"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">ACTVTY111</ns4:IdentificationID>
						</ns4:ActivityIdentification>
						<!--Optional:-->
						<!--<ns4:ActivityDateRepresentation>?</ns4:ActivityDateRepresentation>-->
						<ns4:ActivityDate ns2:id=""ID6"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">
							<ns4:Date ns2:id=""ID7"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">2016-05-25</ns4:Date>
						</ns4:ActivityDate>
						<ns3:TransferActivityCode ns2:id=""ID8"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">RR</ns3:TransferActivityCode>
					</ns3:TransferActivity>
					<ns3:Sender ns2:id=""ID9"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">
						<ns4:OrganizationIdentification ns2:id=""ID10"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">
							<!--Optional:-->
							<ns4:IdentificationID ns2:id=""ID11"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">DADS-LA</ns4:IdentificationID>
						</ns4:OrganizationIdentification>
						<ns4:OrganizationName ns2:id=""ID12"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">DADS-LA</ns4:OrganizationName>
					</ns3:Sender>
					<ns3:Receiver ns2:id=""ID13"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">
						<ns4:OrganizationIdentification ns2:id=""ID14"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">
							<!--Optional:-->
							<ns4:IdentificationID ns2:id=""ID15"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">LTSS</ns4:IdentificationID>
						</ns4:OrganizationIdentification>
						<ns4:OrganizationName ns2:id=""ID16"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">LTSS</ns4:OrganizationName>
					</ns3:Receiver>
				</ns3:TransferHeader>
				<ns3:ReferralID ns2:id=""ID17"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">
					<!--Optional:-->
					<ns4:IdentificationID ns2:id=""ID18"" ns2:metadata=""ID1"" ns2:ref=""ID1"" ns2:relationshipMetadata=""ID1"">[REFERRALID]</ns4:IdentificationID>
				</ns3:ReferralID>
			</ns1:RetrieveReferralInfoRequest>
		</ns:retrieveReferralInfoRequest>
	</soap:Body>
</soap:Envelope>";
            }
        }

        public override void AfterReceiveReply(ref Message reply, object correlationState)
        {
            base.AfterReceiveReply(ref reply, correlationState);
            Repository.SaveResponse(_requestID, Reply);
        }
    }
}