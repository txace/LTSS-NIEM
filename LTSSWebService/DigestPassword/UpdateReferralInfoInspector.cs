using System;

namespace DigestPasswordNS
{
    public class UpdateReferralInfoInspector : DigestPasswordClientMessageInspector
    {
        private string _reasonForUpdate;
        private string _referralID;

        public UpdateReferralInfoInspector(string userName, string passWord, string ReferralID, string ReasonForUpdate) : base(userName, passWord)
        {
            _referralID = ReferralID;
            _reasonForUpdate = ReasonForUpdate;
        }

        public override Func<string, string> EvaluateResponseTemplate
        {
            get
            {
                var Now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                return new Func<string, string>(x => x.Replace("[ACTIVITYDATE]", Now)
                                                        .Replace("[REASONFORUPDATE]", _reasonForUpdate)
                                                        .Replace("[REASONFORUPDATEDATE]", Now)
                                                        .Replace("[REFERRALSTATUSCODE]", _reasonForUpdate == "SUCCESS" ? "AC" : "PF")
                                                        .Replace("[REFERRALID]", _referralID));
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
	xmlns:ns5=""http://ltss.ee.hhsc.state.tx.us/ltssee/1.1""
	xmlns:ns6=""http://ltss.referralmanagement.hhsc.state.tx.us/extension/1.1""
	xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"">
	<soap:Header>
		[SECURITY]
	</soap:Header>
	<soap:Body>
		<ns:updateReferralInfoRequest>
			<ns1:UpdateReferralInfoRequest>
				<ns3:TransferHeader>
					<ns3:TransferActivity>
						<!--Optional:-->
						<ns4:ActivityIdentification>
							<!--Optional:-->
							<ns4:IdentificationID>100</ns4:IdentificationID>
						</ns4:ActivityIdentification>
						<!--Optional:-->
						<ActivityDate
							xmlns=""http://release.niem.gov/niem/niem-core/3.0/"">
							<DateTime>[ACTIVITYDATE]</DateTime>
						</ActivityDate>
						<ns3:TransferActivityCode>UR</ns3:TransferActivityCode>
					</ns3:TransferActivity>
					<ns3:Sender>
						<ns4:OrganizationIdentification>
							<!--Optional:-->
							<ns4:IdentificationID>DADS-LA</ns4:IdentificationID>
						</ns4:OrganizationIdentification>
						<ns4:OrganizationName>DADS</ns4:OrganizationName>
					</ns3:Sender>
					<ns3:Receiver>
						<ns4:OrganizationIdentification>
							<!--Optional:-->
							<ns4:IdentificationID>101</ns4:IdentificationID>
						</ns4:OrganizationIdentification>
						<ns4:OrganizationName>HHSC</ns4:OrganizationName>
					</ns3:Receiver>
				</ns3:TransferHeader>
				<ns3:ReferralID>
					<!--Optional:-->
					<ns4:IdentificationID>[REFERRALID]</ns4:IdentificationID>
				</ns3:ReferralID>
				<ns5:ReferralStatusCode>[REFERRALSTATUSCODE]</ns5:ReferralStatusCode>
				<!--Optional:-->
				<ns6:ReasonForUpdate>[REASONFORUPDATE]</ns6:ReasonForUpdate>
				<!--Optional:-->
				<ns6:ReferralUpdatedBy>LA</ns6:ReferralUpdatedBy>
				<!--Optional:-->
				<ns6:ReferralUpdatedDate>[REASONFORUPDATEDATE]</ns6:ReferralUpdatedDate>
				<!--Optional:-->
				<ns6:UserComments></ns6:UserComments>
			</ns1:UpdateReferralInfoRequest>
		</ns:updateReferralInfoRequest>
	</soap:Body>
</soap:Envelope>";
            }
        }
    }
}