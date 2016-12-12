using Application.ActionExtensions;
using Application.ExceptionExtensions;
using Application.GenericExtensions;
using Application.IEnumerableExtensions;
using Application.ObjectExtensions;
using DigestPasswordNS;
using LTSSWebService.IServiceExtensions;
using LTSSWebService.LTSSReferralManagementService;
using LTSSWebService.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Xml.Linq;

namespace LTSSWebService
{
    [ServiceContract(Namespace = @"http://ltss.referraldispatcher.hhsc.state.tx.us/interface/1.1")]
    public interface ISendReferralService
    {
        [OperationContract(Action = "http://ltss.referraldispatcher.hhsc.state.tx.us/interface/1.1/sendReferral/")]
        [WebInvoke(Method = "POST",
           UriTemplate = "SendReferralRequest",
            RequestFormat = WebMessageFormat.Xml,
            ResponseFormat = WebMessageFormat.Xml,
           BodyStyle = WebMessageBodyStyle.Bare)]
        Schema.SendReferralResponsePayloadType sendReferralRequest(Schema.SendReferralRequestPayloadType request);
    }

    public class SendReferralService : ISendReferralService, IService
    {
        public static Func<object, string> ExtractDate = x => (x as date)?.Value.ToSafeString() ?? (x as dateTime)?.Value.ToSafeString() ?? (x as gYearMonth)?.Value;

        public static Action<int, string> GetRequest = (RequestID, Request) =>
        {
            var SendReferralRequest = JsonConvert.DeserializeObject<SendReferralRequest>(Request);
            var ReferralID = SendReferralRequest.ReferralID;

            var ReasonForUpdate = "SUCCESS";
            try
            {
                using (var client = new LTSSReferralManagementService.LTSSReferralManagementServicePortTypeClient(DigestPasswordBinding.CreateBinding(),
                    new EndpointAddress(ConfigurationManager.AppSettings["LTSSEndpoint"])))
                {
                    client.Endpoint.EndpointBehaviors.Add(new RetrieveReferralInfoInspector(ConfigurationManager.AppSettings["LTSSUserName"],
                                                                ConfigurationManager.AppSettings["LTSSPassword"], RequestID, ReferralID));

                    System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                    var ReferralInfo = client.retrieveReferralInfo(new LTSSReferralManagementService.RetrieveReferralInfoInputMessage());
                    ((Action)(NormalizeData.Normalize)).FireAndForget();
                }
            }
            catch (Exception e)
            {
                ReasonForUpdate = e.FullException();
                Repository.SaveException(RequestID, e);
            }
            finally
            {
                using (var client = new LTSSReferralManagementService.LTSSReferralManagementServicePortTypeClient(DigestPasswordBinding.CreateBinding(),
                    new EndpointAddress(ConfigurationManager.AppSettings["LTSSEndpoint"])))
                {
                    client.Endpoint.EndpointBehaviors.Add(new UpdateReferralInfoInspector(ConfigurationManager.AppSettings["LTSSUserName"],
                                                                ConfigurationManager.AppSettings["LTSSPassword"], ReferralID, ReasonForUpdate));

                    client.updateReferralInfo(new LTSSReferralManagementService.UpdateReferralInfoInputMessage());
                }
            }
        };

        public static Action<int, string> SaveService = (Action<int, string>)Repository.SaveSendReferralService;

        public XDocument Request { get; set; }

        public string SuccessResponse { get { return @"<s:Envelope xmlns:a=""http://www.w3.org/2005/08/addressing"" xmlns:s=""http://www.w3.org/2003/05/soap-envelope"">
   <s:Body xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
      <sendReferralResponse xmlns=""http://ltss.referraldispatcher.hhsc.state.tx.us/interface/1.1"">
         <SendReferralResponse xmlns=""http://ltss.referraldispatcher.hhsc.state.tx.us/exchange/1.1"">
            <TransferHeader xmlns=""http://ltss.core.hhsc.state.tx.us/ltsscore/1.1"">
               <TransferActivity>
                  <ActivityIdentification xmlns=""http://release.niem.gov/niem/niem-core/3.0/"">
                     <IdentificationID>1001</IdentificationID>
                  </ActivityIdentification>
                  <ActivityDate xmlns=""http://release.niem.gov/niem/niem-core/3.0/"">
                     <Date>" + DateTime.Now.ToString("yyyy-MM-dd") + @"</Date>
                  </ActivityDate>
                  <TransferActivityCode>SR</TransferActivityCode>
               </TransferActivity>
               <Sender>
                  <OrganizationIdentification xmlns=""http://release.niem.gov/niem/niem-core/3.0/"">
                     <IdentificationID>LTSS</IdentificationID>
                  </OrganizationIdentification>
                  <OrganizationName xmlns=""http://release.niem.gov/niem/niem-core/3.0/"">LTSS</OrganizationName>
               </Sender>
               <Receiver>
                  <OrganizationIdentification xmlns=""http://release.niem.gov/niem/niem-core/3.0/"">
                     <IdentificationID>DADS-LA</IdentificationID>
                  </OrganizationIdentification>
                  <OrganizationName xmlns=""http://release.niem.gov/niem/niem-core/3.0/"">DADS</OrganizationName>
               </Receiver>
            </TransferHeader>
            <ResponseMetadata xmlns=""http://ltss.core.hhsc.state.tx.us/ltsscore/1.1"">
               <ResponseCode>0000</ResponseCode>
               <ResponseDescriptionText>SUCCESS</ResponseDescriptionText>
            </ResponseMetadata>
         </SendReferralResponse>
      </sendReferralResponse>
   </s:Body>
</s:Envelope>"; } }

        public static void SaveScreenings(IEnumerable<LTSSWebService.LTSSReferralManagementService.ScreeningApplicationType> screenings, ReferralDbContext context)
        {
            // Screening
            var Screenings = screenings?.Select(x => new Screening
            {
                ScreeningID = x?.ScreeningID?.IdentificationID?.Value,
                ApplicationStatusCode = x?.ApplicationStatusCode?.Value.ToSafeString(), // is Enum
                TermsOfUseVersionIndicator = x?.TermsOfUse?.VersionIndicator?.Value,
                TermsOfUseSignedBy = x?.TermsOfUse?.TermsOfUseSignedBy?.Value,
                TermsOfUseSignatureDate = x?.TermsOfUse?.TermsOfUseSignatureDate?.Value,
                TermsOfUseExpiryDate = x?.TermsOfUse?.TermsOfUseExpiryDate?.Value,
                TermsOfUseOrganizationID = x?.TermsOfUse?.OrganizationID?.IdentificationID?.Value,
                TermsOfUseTypeCode = x?.TermsOfUse?.TermsOfUseTypeCode?.Value.ToSafeString(), // is Enum
                TermsOfUseExternalID = x?.TermsOfUse?.ExternalID?.IdentificationID?.Value,
                TermsOfUseStaffName = x?.TermsOfUse?.TermsOfUseStaffName?.Value,
                CreatedBy = x?.CreatedBy?.Value,
                CreateDateTime = x?.CreateDateTime?.Value,
                UpdatedBy = x?.UpdatedBy?.Value,
                UpdateDateTime = x?.UpdateDateTime?.Value,
                DataEnteredBy = x?.DataEnteredBy?.Value,
                EnteredDateTime = x?.EnteredDateTime?.Value,
                Comments = x?.Comments?.Value,
                PersonActiveMilitaryIndicator = x?.ScreeningApplicant?.PersonActiveMilitaryIndicator?.Value.ToSafeString(), // is Enum
                PersonLtssID = x?.ScreeningApplicant?.PersonLtssID?.IdentificationID?.Value,
                PersonVeteranMilitaryIndicator = x?.ScreeningApplicant?.PersonVeteranMilitaryIndicator?.Value.ToSafeString(), // is Enum
                ReferringOrganization = x?.ScreeningApplicant?.AuthorizedRepresentative?.ReferringOrganization?.Value,
                CareRecipientHasCognitiveImpairment = x?.ScreeningApplicant?.CareRecipient?.CareRecipientHasCognitiveImpairment?.Value,
                CareRecipientHasOtherDisabilities = x?.ScreeningApplicant?.CareRecipient?.CareRecipientHasOtherDisabilities?.Value,
                CareRecipientFamilyRelationshipCode = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.FamilyRelationshipCode?.Value.ToSafeString(), // is Enum
                CareRecipientPersonCaretakerDependentCode = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.PersonCaretakerDependentCode?.Value.ToSafeString(), // is Enum
                CareRecipientAssociationDateRangeStartDate = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.AssociationDateRange?.StartDate?.Item.SelectOrDefault(ExtractDate, null),
                CareRecipientAssociationDateRangeEndDate = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.AssociationDateRange?.EndDate?.Item.SelectOrDefault(ExtractDate, null),
                CareRecipientAssociationDescriptionText = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.AssociationDescriptionText?.Value,
                CareRecipientLivesWithCaregiver = x?.ScreeningApplicant?.CareRecipient?.CareRecipientLivesWithCaregiver?.Value.ToSafeString(), // is Enum
                CareRecipientLocationStateUSPostalServiceCode = x?.ScreeningApplicant?.CareRecipient?.LocationStateUSPostalServiceCode?.Value.ToSafeString(), // is Enum
                CareRecipientLocationCountyCode = x?.ScreeningApplicant?.CareRecipient?.LocationCountyCode?.Value.ToSafeString(), // is Enum
                ScreeningQuestionnaireVersionIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.VersionIndicator?.Value,
                ApplicantIntellectualDisabilityIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantIntellectualDisabilityIndicator?.Value,
                ApplicantAutismOrPervasiveDisorderIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantAutismOrPervasiveDisorderIndicator?.Value,
                ApplicantCognitiveImpairmentIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantCognitiveImpairmentIndicator?.Value,
                ApplicantRequiresAssistanceMeetingPersonalNeedsIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantRequiresAssistanceMeetingPersonalNeedsIndicator?.Value,
                ApplicantCaregiverForExistingConditionsIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantCaregiverForExistingConditionsIndicator?.Value,
                ApplicantGetsCareIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantGetsCareIndicator?.Value,
                ApplicantMentalHealthDiagnosisIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantMentalHealthDiagnosisIndicator?.Value,
                ApplicantNeedsAssistanceCallingForHelpIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantNeedsAssistanceCallingForHelpIndicator?.Value,
                ApplicantPhysicalDisabilityIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantPhysicalDisabilityIndicator?.Value,
                ApplicantSubstanceAbuseIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantSubstanceAbuseIndicator?.Value,
                ApplicantHasQuestionsAboutMedicareOrLTCInsuranceIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantHasQuestionsAboutMedicareOrLTCInsuranceIndicator?.Value.ToSafeString(), // is Enum
                ApplicantDroppedActivitiesAndInterestsIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantDroppedActivitiesAndInterestsIndicator?.Value.ToSafeString(), // is Enum
                ApplicantFeelsFullOfEnergyIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantFeelsFullOfEnergyIndicator?.Value.ToSafeString(), // is Enum
                ApplicantNumberOfInstancesOfAlcoholConsumptionInPastOneYearCode = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantNumberOfInstancesOfAlcoholConsumptionInPastOneYearCode?.Value.ToSafeString(), // is Enum
                ApplicantLikelyhoodOfMovingToNursingFacilityCode = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantLikelyhoodOfMovingToNursingFacilityCode?.Value.ToSafeString(), // is Enum
                ApplicantThinksWonderfulToBeAliveNowIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantThinksWonderfulToBeAliveNowIndicator?.Value.ToSafeString(), // is Enum
                ApplicantHasNarcoticsPrescriptionInPast30DaysIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantHasNarcoticsPrescriptionInPast30DaysIndicator?.Value,
                ApplicantHasDiabetesMedicationInPast30DaysIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantHasDiabetesMedicationInPast30DaysIndicator?.Value,
                ApplicantHasMedicationForSleepInPast30DaysIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantHasMedicationForSleepInPast30DaysIndicator?.Value,
                ApplicantHasMedicationForAnxietyInPast30DaysIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantHasMedicationForAnxietyInPast30DaysIndicator?.Value,
                ApplicantHasMedicationForHeartDiseasesOrBloodPressureInPast30DaysIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantHasMedicationForHeartDiseasesOrBloodPressureInPast30DaysIndicator?.Value,
                ApplicantHasConsumedAlcoholInPast30DaysIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantHasConsumedAlcoholInPast30DaysIndicator?.Value,
                ApplicantFeelsLifeIsEmptyIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantFeelsLifeIsEmptyIndicator?.Value.ToSafeString(), // is Enum
                ApplicantInterestedInSTARPLUSIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantInterestedInSTARPLUSIndicator?.Value,
                ApplicantInterestedInPACEIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantInterestedInPACEIndicator?.Value,
                ApplicantInterestedInCLASSIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantInterestedInCLASSIndicator?.Value,
                ApplicantInterestednDBMDIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantInterestednDBMDIndicator?.Value,
                AplicantInterestedInMDCPIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.AplicantInterestedInMDCPIndicator?.Value,
                ApplicantCaregiverNeedsInformationIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantCaregiverNeedsInformationIndicator?.Value.ToSafeString(), // is Enum
                ApplicantGetsCareFromPersonWhoNeedsHelpIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantGetsCareFromPersonWhoNeedsHelpIndicator?.Value.ToSafeString(), // is Enum
                ApplicantRequiresLicensedNurseSupervisionIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantRequiresLicensedNurseSupervisionIndicator?.Value,
                ApplicantDeafAndBlindIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantDeafAndBlindIndicator?.Value,
                ApplicantDevelopmentalDisabilityIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantDevelopmentalDisabilityIndicator?.Value,
                ApplicantHasConsentToReleaseOfInformationIndicator = x?.ScreeningApplicant?.ScreeningQuestionnaire?.ApplicantHasConsentToReleaseOfInformationIndicator?.Value,
                LivesOutsideStateTemporarilyIndicator = x?.ScreeningApplicant?.LivesOutsideStateTemporarilyIndicator?.Value.ToSafeString(), // is Enum
                HasNoFixedAddressIndicator = x?.ScreeningApplicant?.HasNoFixedAddressIndicator?.Value.ToSafeString(), // is Enum
                PersonMedicaidID = x?.ScreeningApplicant?.MedicaidInformation?.PersonMedicaidID?.IdentificationID?.Value,
                MedicaidInformationDateTime = x?.ScreeningApplicant?.MedicaidInformation?.DateTime?.Value,
                MedicaidInformationDateTime1 = x?.ScreeningApplicant?.MedicaidInformation?.DateTime1?.Value,
                CurrentInterestListPersonCsilID = x?.ScreeningApplicant?.CurrentInterestList?.PersonCsilID?.IdentificationID?.Value,
                ApplicantHasTriCareInsurance = x?.ScreeningApplicant?.ApplicantHasTriCareInsurance?.Value,
                ApplicantScreeningObservations = x?.ScreeningApplicant?.ApplicantScreeningObservations?.Value,
                TypeOfMedicalInsurance = x?.ScreeningApplicant?.TypeOfMedicalInsurance?.Value,
                ScreeningApplicantComments = x?.ScreeningApplicant?.Comments?.Value,
                FamilyRelationshipCode = x?.ScreeningApplicant?.PersonAssociation?.FamilyRelationshipCode?.Value.ToSafeString(), // is Enum
                PersonCaretakerDependentCode = x?.ScreeningApplicant?.PersonAssociation?.PersonCaretakerDependentCode?.Value.ToSafeString(), // is Enum
                AssociationDateRangeStartDate = x?.ScreeningApplicant?.PersonAssociation?.AssociationDateRange?.StartDate?.Item.SelectOrDefault(ExtractDate, null),
                AssociationDateRangeEndDate = x?.ScreeningApplicant?.PersonAssociation?.AssociationDateRange?.EndDate?.Item.SelectOrDefault(ExtractDate, null),
                AssociationDescriptionText = x?.ScreeningApplicant?.PersonAssociation?.AssociationDescriptionText?.Value,
                PersonMedicareID = x?.ScreeningApplicant?.PersonMedicareID?.IdentificationID?.Value,
                PersonPreferredLanguage = x?.ScreeningApplicant?.PersonPreferredLanguage?.Value.ToSafeString(), // is Enum
                USNaturalizedCitizenIndicator = x?.ScreeningApplicant?.USNaturalizedCitizenIndicator?.Value,
                PersonMarriedIndicator = x?.ScreeningApplicant?.PersonMarriedIndicator?.Value,
                PersonUSVeteranIndicator = x?.ScreeningApplicant?.PersonUSVeteranIndicator?.Value,
                PersonTiersID = x?.ScreeningApplicant?.PersonTiersID?.IdentificationID?.Value,
                PersonCsilID = x?.ScreeningApplicant?.PersonCsilID?.IdentificationID?.Value,
                PersonCmbhsID = x?.ScreeningApplicant?.PersonCmbhsID?.IdentificationID?.Value
            })
            ?.ToArray();

            context.AddRange<Screening>(Screenings);

            // Contact Entity
            // AuthorizedRepresentative
            var AuthorizedRepresentatives = screenings?.Select(x => new ContactEntity
            {
                PersonAgeMeasure = x?.ScreeningApplicant?.AuthorizedRepresentative?.PersonAgeMeasure?.Item?.Value,
                PersonBirthDate = x?.ScreeningApplicant?.AuthorizedRepresentative?.PersonBirthDate?.Item.SelectOrDefault(ExtractDate, null),
                EthnicityCode = x?.ScreeningApplicant?.AuthorizedRepresentative?.Item?.Value.ToSafeString(), // is Enum
                PersonLivingIndicator = x?.ScreeningApplicant?.AuthorizedRepresentative?.PersonLivingIndicator?.Value,
                PersonNamePrefixText = x?.ScreeningApplicant?.AuthorizedRepresentative?.PersonName?.PersonNamePrefixText?.Value,
                PersonGivenName = x?.ScreeningApplicant?.AuthorizedRepresentative?.PersonName?.PersonGivenName?.Value,
                PersonMiddleName = x?.ScreeningApplicant?.AuthorizedRepresentative?.PersonName?.PersonMiddleName?.Value,
                PersonSurName = x?.ScreeningApplicant?.AuthorizedRepresentative?.PersonName?.PersonSurName?.Value,
                PersonNameSuffixText = x?.ScreeningApplicant?.AuthorizedRepresentative?.PersonName?.PersonNameSuffixText?.Value,
                PersonFullName = x?.ScreeningApplicant?.AuthorizedRepresentative?.PersonName?.PersonFullName?.Value,
                PersonSurNamePrefixText = x?.ScreeningApplicant?.AuthorizedRepresentative?.PersonName?.PersonSurNamePrefixText?.Value,
                RACCode = x?.ScreeningApplicant?.AuthorizedRepresentative?.Item1?.Value.ToSafeString(), // is Enum
                SEXCode = x?.ScreeningApplicant?.AuthorizedRepresentative?.Item2?.Value.ToSafeString(), // is Enum
                PersonSSNIdentification = x?.ScreeningApplicant?.AuthorizedRepresentative?.PersonSSNIdentification?.IdentificationID?.Value,
                PersonUSCitizenIndicator = x?.ScreeningApplicant?.AuthorizedRepresentative?.PersonUSCitizenIndicator?.Value,
            })
            ?.ToArray();

            //CareRecipient
            var CareRecipients = screenings?.Select(x => new ContactEntity
            {
                PersonAgeMeasure = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.Person?.PersonAgeMeasure?.Item?.Value,
                PersonBirthDate = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.Person?.PersonBirthDate?.Item.SelectOrDefault(ExtractDate, null),
                EthnicityCode = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.Person?.Item?.Value.ToSafeString(), // is Enum
                PersonLivingIndicator = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.Person?.PersonLivingIndicator?.Value,
                PersonNamePrefixText = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.Person?.PersonName?.PersonNamePrefixText?.Value,
                PersonGivenName = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.Person?.PersonName?.PersonGivenName?.Value,
                PersonMiddleName = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.Person?.PersonName?.PersonMiddleName?.Value,
                PersonSurName = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.Person?.PersonName?.PersonSurName?.Value,
                PersonNameSuffixText = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.Person?.PersonName?.PersonNameSuffixText?.Value,
                PersonFullName = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.Person?.PersonName?.PersonFullName?.Value,
                PersonSurNamePrefixText = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.Person?.PersonName?.PersonSurNamePrefixText?.Value,
                RACCode = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.Person?.Item1?.Value.ToSafeString(), // is Enum
                SEXCode = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.Person?.Item2?.Value.ToSafeString(), // is Enum
                PersonSSNIdentification = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.Person?.PersonSSNIdentification?.IdentificationID?.Value,
                PersonUSCitizenIndicator = x?.ScreeningApplicant?.CareRecipient?.PersonAssociation?.Person?.PersonUSCitizenIndicator?.Value,
            })
            ?.ToArray();

            //PersonAssociation
            var PersonAssociations = screenings?.Select(x => new ContactEntity
            {
                PersonAgeMeasure = x?.ScreeningApplicant?.PersonAssociation?.Person?.PersonAgeMeasure?.Item?.Value,
                PersonBirthDate = x?.ScreeningApplicant?.PersonAssociation?.Person?.PersonBirthDate?.Item.SelectOrDefault(ExtractDate, null),
                EthnicityCode = x?.ScreeningApplicant?.PersonAssociation?.Person?.Item?.Value.ToSafeString(), // is Enum
                PersonLivingIndicator = x?.ScreeningApplicant?.PersonAssociation?.Person?.PersonLivingIndicator?.Value,
                PersonNamePrefixText = x?.ScreeningApplicant?.PersonAssociation?.Person?.PersonName?.PersonNamePrefixText?.Value,
                PersonGivenName = x?.ScreeningApplicant?.PersonAssociation?.Person?.PersonName?.PersonGivenName?.Value,
                PersonMiddleName = x?.ScreeningApplicant?.PersonAssociation?.Person?.PersonName?.PersonMiddleName?.Value,
                PersonSurName = x?.ScreeningApplicant?.PersonAssociation?.Person?.PersonName?.PersonSurName?.Value,
                PersonNameSuffixText = x?.ScreeningApplicant?.PersonAssociation?.Person?.PersonName?.PersonNameSuffixText?.Value,
                PersonFullName = x?.ScreeningApplicant?.PersonAssociation?.Person?.PersonName?.PersonFullName?.Value,
                PersonSurNamePrefixText = x?.ScreeningApplicant?.PersonAssociation?.Person?.PersonName?.PersonSurNamePrefixText?.Value,
                RACCode = x?.ScreeningApplicant?.PersonAssociation?.Person?.Item1?.Value.ToSafeString(), // is Enum
                SEXCode = x?.ScreeningApplicant?.PersonAssociation?.Person?.Item2?.Value.ToSafeString(), // is Enum
                PersonSSNIdentification = x?.ScreeningApplicant?.PersonAssociation?.Person?.PersonSSNIdentification?.IdentificationID?.Value,
                PersonUSCitizenIndicator = x?.ScreeningApplicant?.PersonAssociation?.Person?.PersonUSCitizenIndicator?.Value,
            })
            ?.ToArray();

            //ScreeningApplicant
            var ScreeningApplicants = screenings?.Select(x => new ContactEntity
            {
                PersonAgeMeasure = x?.ScreeningApplicant?.PersonAgeMeasure?.Item?.Value,
                PersonBirthDate = x?.ScreeningApplicant?.PersonBirthDate?.Item.SelectOrDefault(ExtractDate, null),
                EthnicityCode = x?.ScreeningApplicant?.Item?.Value.ToSafeString(), // is Enum
                PersonLivingIndicator = x?.ScreeningApplicant?.PersonLivingIndicator?.Value,
                PersonNamePrefixText = x?.ScreeningApplicant?.PersonName?.PersonNamePrefixText?.Value,
                PersonGivenName = x?.ScreeningApplicant?.PersonName?.PersonGivenName?.Value,
                PersonMiddleName = x?.ScreeningApplicant?.PersonName?.PersonMiddleName?.Value,
                PersonSurName = x?.ScreeningApplicant?.PersonName?.PersonSurName?.Value,
                PersonNameSuffixText = x?.ScreeningApplicant?.PersonName?.PersonNameSuffixText?.Value,
                PersonFullName = x?.ScreeningApplicant?.PersonName?.PersonFullName?.Value,
                PersonSurNamePrefixText = x?.ScreeningApplicant?.PersonName?.PersonSurNamePrefixText?.Value,
                RACCode = x?.ScreeningApplicant?.Item1?.Value.ToSafeString(), // is Enum
                SEXCode = x?.ScreeningApplicant?.Item2?.Value.ToSafeString(), // is Enum
                PersonSSNIdentification = x?.ScreeningApplicant?.PersonSSNIdentification?.IdentificationID?.Value,
                PersonUSCitizenIndicator = x?.ScreeningApplicant?.PersonUSCitizenIndicator?.Value,
            })
            ?.ToArray();

            // CurrentEnrollment
            var CurrentEnrollments = screenings?.Select(x =>
                x?.ScreeningApplicant?.CurrentEnrollment?.Select(y => new Enrollment
                {
                    IdentificationID = y?.EnrollmentID?.IdentificationID?.Value,
                    ProgramName = y?.ProgramName?.Value,
                    PlanName = y?.PlanName?.Value,
                    StartDate = y?.EnrollmentDateRange?.StartDate?.Item.SelectOrDefault(ExtractDate, null),
                    EndDate = y?.EnrollmentDateRange?.EndDate?.Item.SelectOrDefault(ExtractDate, null),
                    ScreeningID = x?.ScreeningID?.IdentificationID?.Value
                }))
                ?.SelectManySafely(x => x)
                ?.ToArray();

            context.AddRange<Enrollment>(CurrentEnrollments);

            // ContactInformation
            var ContactInformations = screenings?.Select(x => new
            {
                x?.ScreeningID,
                Contacts = x?.ScreeningApplicant?.ContactInformation
                .Select(y => new ContactEntity
                {
                    CategoryCode = y?.ContactInformationCategoryCode?.Value.ToSafeString(), // is Enum
                    ContactEmailID = (y?.Item as @string)?.Value,
                    ContactMailingAddress = (y?.Item as AddressType)?.AddressFullText?.Value,
                    ContactTelephoneNumber = (y?.Item as TelephoneNumberType)?.Item?.TelephoneNumberFullID?.Value,
                    ContactWebsiteURI = (y?.Item as anyURI)?.Value,
                    PersonAgeMeasure = y?.ContactEntity?.Item?.PersonAgeMeasure?.Item?.Value,
                    PersonBirthDate = y?.ContactEntity?.Item?.PersonBirthDate?.Item.SelectOrDefault(ExtractDate, null),
                    EthnicityCode = y?.ContactEntity?.Item?.Item?.Value.ToSafeString(), // is Enum
                    PersonLivingIndicator = y?.ContactEntity?.Item?.PersonLivingIndicator?.Value,
                    PersonNamePrefixText = y?.ContactEntity?.Item?.PersonName?.PersonNamePrefixText?.Value,
                    PersonGivenName = y?.ContactEntity?.Item?.PersonName?.PersonGivenName?.Value,
                    PersonMiddleName = y?.ContactEntity?.Item?.PersonName?.PersonMiddleName?.Value,
                    PersonSurName = y?.ContactEntity?.Item?.PersonName?.PersonSurName?.Value,
                    PersonNameSuffixText = y?.ContactEntity?.Item?.PersonName?.PersonNameSuffixText?.Value,
                    PersonFullName = y?.ContactEntity?.Item?.PersonName?.PersonFullName?.Value,
                    PersonSurNamePrefixText = y?.ContactEntity?.Item?.PersonName?.PersonSurNamePrefixText?.Value,
                    RACCode = y?.ContactEntity?.Item?.Item1?.Value.ToSafeString(), // is Enum
                    SEXCode = y?.ContactEntity?.Item?.Item2?.Value.ToSafeString(), // is Enum
                    PersonSSNIdentification = y?.ContactEntity?.Item?.PersonSSNIdentification?.IdentificationID?.Value,
                    PersonUSCitizenIndicator = y?.ContactEntity?.Item?.PersonUSCitizenIndicator?.Value,
                })
            });

            var RSSOOLCI = screenings?.Select(x =>
                x?.ReferralsSent?.Select(y =>
                    y?.SourceOrganization?.OrganizationLocation
                    .Select(z => new ContactEntity
                    {
                        EthnicityCode = z?.LocationContactInformation?.ContactEntity?.Item?.Item?.Value.ToSafeString(), // is Enum
                        RACCode = z?.LocationContactInformation?.ContactEntity?.Item?.Item1?.Value.ToSafeString(), // is Enum
                        SEXCode = z?.LocationContactInformation?.ContactEntity?.Item?.Item2?.Value.ToSafeString(), // is Enum
                        PersonAgeMeasure = z?.LocationContactInformation?.ContactEntity?.Item?.PersonAgeMeasure?.Item?.Value,
                        PersonBirthDate = z?.LocationContactInformation?.ContactEntity?.Item?.PersonBirthDate?.Item.SelectOrDefault(ExtractDate, null),
                        PersonLivingIndicator = z?.LocationContactInformation?.ContactEntity?.Item?.PersonLivingIndicator?.Value,
                        PersonFullName = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonFullName?.Value,
                        PersonGivenName = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonGivenName?.Value,
                        PersonMiddleName = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonMiddleName?.Value,
                        PersonNamePrefixText = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonNamePrefixText?.Value,
                        PersonNameSuffixText = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonNameSuffixText?.Value,
                        PersonSurName = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonSurName?.Value,
                        PersonSurNamePrefixText = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonSurNamePrefixText?.Value,
                        PersonSSNIdentification = z?.LocationContactInformation?.ContactEntity?.Item?.PersonSSNIdentification?.IdentificationID?.Value,
                        PersonUSCitizenIndicator = z?.LocationContactInformation?.ContactEntity?.Item?.PersonUSCitizenIndicator?.Value,
                        ContactEmailID = (z?.LocationContactInformation?.Item as @string)?.Value,
                        ContactMailingAddress = (z?.LocationContactInformation?.Item as AddressType)?.AddressFullText?.Value,
                        ContactTelephoneNumber = (z?.LocationContactInformation?.Item as TelephoneNumberType)?.Item?.TelephoneNumberFullID?.Value,
                        ContactWebsiteURI = (z?.LocationContactInformation?.Item as anyURI)?.Value,
                    })));

            var RSSOCI = screenings?.Select(x =>
                x?.ReferralsSent?.Select(y =>
                    y?.SourceOrganization?.OrganizationPrimaryContactInformation?
                    .Select(z => new ContactEntity
                    {
                        ContactEmailID = (z?.Item as @string)?.Value,
                        ContactMailingAddress = (z?.Item as AddressType)?.AddressFullText?.Value,
                        ContactTelephoneNumber = (z?.Item as TelephoneNumberType)?.Item?.TelephoneNumberFullID?.Value,
                        ContactWebsiteURI = (z?.Item as anyURI)?.Value,
                        PersonAgeMeasure = z?.ContactEntity?.Item?.PersonAgeMeasure?.Item?.Value,
                        PersonBirthDate = z?.ContactEntity?.Item?.PersonBirthDate?.Item.SelectOrDefault(ExtractDate, null),
                        EthnicityCode = z?.ContactEntity?.Item?.Item?.Value.ToSafeString(), // is Enum
                        PersonLivingIndicator = z?.ContactEntity?.Item?.PersonLivingIndicator?.Value,
                        PersonFullName = z?.ContactEntity?.Item?.PersonName?.PersonFullName?.Value,
                        PersonGivenName = z?.ContactEntity?.Item?.PersonName?.PersonGivenName?.Value,
                        PersonMiddleName = z?.ContactEntity?.Item?.PersonName?.PersonMiddleName?.Value,
                        PersonNamePrefixText = z?.ContactEntity?.Item?.PersonName?.PersonNamePrefixText?.Value,
                        PersonNameSuffixText = z?.ContactEntity?.Item?.PersonName?.PersonNameSuffixText?.Value,
                        PersonSurName = z?.ContactEntity?.Item?.PersonName?.PersonSurName?.Value,
                        PersonSurNamePrefixText = z?.ContactEntity?.Item?.PersonName?.PersonSurNamePrefixText?.Value,
                        RACCode = z?.ContactEntity?.Item?.Item1?.Value.ToSafeString(), // is Enum
                        SEXCode = z?.ContactEntity?.Item?.Item2?.Value.ToSafeString(), // is Enum
                        PersonSSNIdentification = z?.ContactEntity?.Item?.PersonSSNIdentification?.IdentificationID?.Value,
                        PersonUSCitizenIndicator = z?.ContactEntity?.Item?.PersonUSCitizenIndicator?.Value,
                    })));

            var RSDOOLCI = screenings?.Select(x =>
                x?.ReferralsSent?.Select(y =>
                    y?.DestinationOrganization?.OrganizationLocation?
                    .Select(z => new ContactEntity
                    {
                        EthnicityCode = z?.LocationContactInformation?.ContactEntity?.Item?.Item?.Value.ToSafeString(), // is Enum
                        RACCode = z?.LocationContactInformation?.ContactEntity?.Item?.Item1?.Value.ToSafeString(), // is Enum
                        SEXCode = z?.LocationContactInformation?.ContactEntity?.Item?.Item2?.Value.ToSafeString(), // is Enum
                        PersonAgeMeasure = z?.LocationContactInformation?.ContactEntity?.Item?.PersonAgeMeasure?.Item?.Value,
                        PersonBirthDate = z?.LocationContactInformation?.ContactEntity?.Item?.PersonBirthDate?.Item.SelectOrDefault(ExtractDate, null),
                        PersonLivingIndicator = z?.LocationContactInformation?.ContactEntity?.Item?.PersonLivingIndicator?.Value,
                        PersonFullName = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonFullName?.Value,
                        PersonGivenName = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonGivenName?.Value,
                        PersonMiddleName = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonMiddleName?.Value,
                        PersonNamePrefixText = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonNamePrefixText?.Value,
                        PersonNameSuffixText = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonNameSuffixText?.Value,
                        PersonSurName = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonSurName?.Value,
                        PersonSurNamePrefixText = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonSurNamePrefixText?.Value,
                        PersonSSNIdentification = z?.LocationContactInformation?.ContactEntity?.Item?.PersonSSNIdentification?.IdentificationID?.Value,
                        PersonUSCitizenIndicator = z?.LocationContactInformation?.ContactEntity?.Item?.PersonUSCitizenIndicator?.Value,
                        ContactEmailID = (z?.LocationContactInformation?.Item as @string)?.Value,
                        ContactMailingAddress = (z?.LocationContactInformation?.Item as AddressType)?.AddressFullText?.Value,
                        ContactTelephoneNumber = (z?.LocationContactInformation?.Item as TelephoneNumberType)?.Item?.TelephoneNumberFullID?.Value,
                        ContactWebsiteURI = (z?.LocationContactInformation?.Item as anyURI)?.Value,
                    })));

            var RSDOCI = screenings?.Select(x =>
                x?.ReferralsSent?.Select(y =>
                    y?.DestinationOrganization?.OrganizationPrimaryContactInformation?
                    .Select(z => new ContactEntity
                    {
                        ContactEmailID = (z.Item as @string)?.Value,
                        ContactMailingAddress = (z.Item as AddressType)?.AddressFullText?.Value,
                        ContactTelephoneNumber = (z.Item as TelephoneNumberType)?.Item?.TelephoneNumberFullID?.Value,
                        ContactWebsiteURI = (z.Item as anyURI)?.Value,
                        PersonAgeMeasure = z?.ContactEntity?.Item?.PersonAgeMeasure?.Item?.Value,
                        PersonBirthDate = z?.ContactEntity?.Item?.PersonBirthDate?.Item.SelectOrDefault(ExtractDate, null),
                        EthnicityCode = z?.ContactEntity?.Item?.Item?.Value.ToSafeString(), // is Enum
                        PersonLivingIndicator = z?.ContactEntity?.Item?.PersonLivingIndicator?.Value,
                        PersonFullName = z?.ContactEntity?.Item?.PersonName?.PersonFullName?.Value,
                        PersonGivenName = z?.ContactEntity?.Item?.PersonName?.PersonGivenName?.Value,
                        PersonMiddleName = z?.ContactEntity?.Item?.PersonName?.PersonMiddleName?.Value,
                        PersonNamePrefixText = z?.ContactEntity?.Item?.PersonName?.PersonNamePrefixText?.Value,
                        PersonNameSuffixText = z?.ContactEntity?.Item?.PersonName?.PersonNameSuffixText?.Value,
                        PersonSurName = z?.ContactEntity?.Item?.PersonName?.PersonSurName?.Value,
                        PersonSurNamePrefixText = z?.ContactEntity?.Item?.PersonName?.PersonSurNamePrefixText?.Value,
                        RACCode = z?.ContactEntity?.Item?.Item1?.Value.ToSafeString(), // is Enum
                        SEXCode = z?.ContactEntity?.Item?.Item2?.Value.ToSafeString(), // is Enum
                        PersonSSNIdentification = z?.ContactEntity?.Item?.PersonSSNIdentification?.IdentificationID?.Value,
                        PersonUSCitizenIndicator = z?.ContactEntity?.Item?.PersonUSCitizenIndicator?.Value,
                    })));

            var RISOOLCI = screenings?.Select(x =>
                x?.ReferralsIdentified?.Select(y =>
                    y?.SourceOrganization?.OrganizationLocation?
                    .Select(z => new ContactEntity
                    {
                        EthnicityCode = z?.LocationContactInformation?.ContactEntity?.Item?.Item?.Value.ToSafeString(), // is Enum
                        RACCode = z?.LocationContactInformation?.ContactEntity?.Item?.Item1?.Value.ToSafeString(), // is Enum
                        SEXCode = z?.LocationContactInformation?.ContactEntity?.Item?.Item2?.Value.ToSafeString(), // is Enum
                        PersonAgeMeasure = z?.LocationContactInformation?.ContactEntity?.Item?.PersonAgeMeasure?.Item?.Value,
                        PersonBirthDate = z?.LocationContactInformation?.ContactEntity?.Item?.PersonBirthDate?.Item.SelectOrDefault(ExtractDate, null),
                        PersonLivingIndicator = z?.LocationContactInformation?.ContactEntity?.Item?.PersonLivingIndicator?.Value,
                        PersonFullName = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonFullName?.Value,
                        PersonGivenName = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonGivenName?.Value,
                        PersonMiddleName = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonMiddleName?.Value,
                        PersonNamePrefixText = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonNamePrefixText?.Value,
                        PersonNameSuffixText = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonNameSuffixText?.Value,
                        PersonSurName = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonSurName?.Value,
                        PersonSurNamePrefixText = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonSurNamePrefixText?.Value,
                        PersonSSNIdentification = z?.LocationContactInformation?.ContactEntity?.Item?.PersonSSNIdentification?.IdentificationID?.Value,
                        PersonUSCitizenIndicator = z?.LocationContactInformation?.ContactEntity?.Item?.PersonUSCitizenIndicator?.Value,
                        ContactEmailID = (z?.LocationContactInformation?.Item as @string)?.Value,
                        ContactMailingAddress = (z?.LocationContactInformation?.Item as AddressType)?.AddressFullText?.Value,
                        ContactTelephoneNumber = (z?.LocationContactInformation?.Item as TelephoneNumberType)?.Item?.TelephoneNumberFullID?.Value,
                        ContactWebsiteURI = (z?.LocationContactInformation?.Item as anyURI)?.Value,
                    })));

            var RISOCI = screenings?.Select(x =>
                x?.ReferralsIdentified?.Select(y =>
                    y?.SourceOrganization?.OrganizationPrimaryContactInformation?
                    .Select(z => new ContactEntity
                    {
                        ContactEmailID = (z.Item as @string)?.Value,
                        ContactMailingAddress = (z?.Item as AddressType)?.AddressFullText?.Value,
                        ContactTelephoneNumber = (z?.Item as TelephoneNumberType)?.Item?.TelephoneNumberFullID?.Value,
                        ContactWebsiteURI = (z?.Item as anyURI)?.Value,
                        PersonAgeMeasure = z?.ContactEntity?.Item?.PersonAgeMeasure?.Item?.Value,
                        PersonBirthDate = z?.ContactEntity?.Item?.PersonBirthDate?.Item.SelectOrDefault(ExtractDate, null),
                        EthnicityCode = z?.ContactEntity?.Item?.Item?.Value.ToSafeString(), // is Enum
                        PersonLivingIndicator = z?.ContactEntity?.Item?.PersonLivingIndicator?.Value,
                        PersonFullName = z?.ContactEntity?.Item?.PersonName?.PersonFullName?.Value,
                        PersonGivenName = z?.ContactEntity?.Item?.PersonName?.PersonGivenName?.Value,
                        PersonMiddleName = z?.ContactEntity?.Item?.PersonName?.PersonMiddleName?.Value,
                        PersonNamePrefixText = z?.ContactEntity?.Item?.PersonName?.PersonNamePrefixText?.Value,
                        PersonNameSuffixText = z?.ContactEntity?.Item?.PersonName?.PersonNameSuffixText?.Value,
                        PersonSurName = z?.ContactEntity?.Item?.PersonName?.PersonSurName?.Value,
                        PersonSurNamePrefixText = z?.ContactEntity?.Item?.PersonName?.PersonSurNamePrefixText?.Value,
                        RACCode = z?.ContactEntity?.Item?.Item1?.Value.ToSafeString(), // is Enum
                        SEXCode = z?.ContactEntity?.Item?.Item2?.Value.ToSafeString(), // is Enum
                        PersonSSNIdentification = z?.ContactEntity?.Item?.PersonSSNIdentification?.IdentificationID?.Value,
                        PersonUSCitizenIndicator = z?.ContactEntity?.Item?.PersonUSCitizenIndicator?.Value,
                    })));

            var RIDOOLCI = screenings?.Select(x =>
                x?.ReferralsIdentified?.Select(y =>
                    y?.DestinationOrganization?.OrganizationLocation?
                    .Select(z => new ContactEntity
                    {
                        EthnicityCode = z?.LocationContactInformation?.ContactEntity?.Item?.Item?.Value.ToSafeString(), // is Enum
                        RACCode = z?.LocationContactInformation?.ContactEntity?.Item?.Item1?.Value.ToSafeString(), // is Enum
                        SEXCode = z?.LocationContactInformation?.ContactEntity?.Item?.Item2?.Value.ToSafeString(), // is Enum
                        PersonAgeMeasure = z?.LocationContactInformation?.ContactEntity?.Item?.PersonAgeMeasure?.Item?.Value,
                        PersonBirthDate = z?.LocationContactInformation?.ContactEntity?.Item?.PersonBirthDate?.Item.SelectOrDefault(ExtractDate, null),
                        PersonLivingIndicator = z?.LocationContactInformation?.ContactEntity?.Item?.PersonLivingIndicator?.Value,
                        PersonFullName = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonFullName?.Value,
                        PersonGivenName = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonGivenName?.Value,
                        PersonMiddleName = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonMiddleName?.Value,
                        PersonNamePrefixText = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonNamePrefixText?.Value,
                        PersonNameSuffixText = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonNameSuffixText?.Value,
                        PersonSurName = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonSurName?.Value,
                        PersonSurNamePrefixText = z?.LocationContactInformation?.ContactEntity?.Item?.PersonName?.PersonSurNamePrefixText?.Value,
                        PersonSSNIdentification = z?.LocationContactInformation?.ContactEntity?.Item?.PersonSSNIdentification?.IdentificationID?.Value,
                        PersonUSCitizenIndicator = z?.LocationContactInformation?.ContactEntity?.Item?.PersonUSCitizenIndicator?.Value,
                        ContactEmailID = (z?.LocationContactInformation?.Item as @string)?.Value,
                        ContactMailingAddress = (z?.LocationContactInformation?.Item as AddressType)?.AddressFullText?.Value,
                        ContactTelephoneNumber = (z?.LocationContactInformation?.Item as TelephoneNumberType)?.Item?.TelephoneNumberFullID?.Value,
                        ContactWebsiteURI = (z?.LocationContactInformation?.Item as anyURI)?.Value,
                    })));

            var RIDOCI = screenings?.Select(x =>
                x?.ReferralsIdentified?.Select(y =>
                    y?.DestinationOrganization?.OrganizationPrimaryContactInformation?
                    .Select(z => new ContactEntity
                    {
                        ContactEmailID = (z.Item as @string)?.Value,
                        ContactMailingAddress = (z?.Item as AddressType)?.AddressFullText?.Value,
                        ContactTelephoneNumber = (z?.Item as TelephoneNumberType)?.Item?.TelephoneNumberFullID?.Value,
                        ContactWebsiteURI = (z?.Item as anyURI)?.Value,
                        PersonAgeMeasure = z?.ContactEntity?.Item?.PersonAgeMeasure?.Item?.Value,
                        PersonBirthDate = z?.ContactEntity?.Item?.PersonBirthDate?.Item.SelectOrDefault(ExtractDate, null),
                        EthnicityCode = z?.ContactEntity?.Item?.Item?.Value.ToSafeString(), // is Enum
                        PersonLivingIndicator = z?.ContactEntity?.Item?.PersonLivingIndicator?.Value,
                        PersonFullName = z?.ContactEntity?.Item?.PersonName?.PersonFullName?.Value,
                        PersonGivenName = z?.ContactEntity?.Item?.PersonName?.PersonGivenName?.Value,
                        PersonMiddleName = z?.ContactEntity?.Item?.PersonName?.PersonMiddleName?.Value,
                        PersonNamePrefixText = z?.ContactEntity?.Item?.PersonName?.PersonNamePrefixText?.Value,
                        PersonNameSuffixText = z?.ContactEntity?.Item?.PersonName?.PersonNameSuffixText?.Value,
                        PersonSurName = z?.ContactEntity?.Item?.PersonName?.PersonSurName?.Value,
                        PersonSurNamePrefixText = z?.ContactEntity?.Item?.PersonName?.PersonSurNamePrefixText?.Value,
                        RACCode = z?.ContactEntity?.Item?.Item1?.Value.ToSafeString(), // is Enum
                        SEXCode = z?.ContactEntity?.Item?.Item2?.Value.ToSafeString(), // is Enum
                        PersonSSNIdentification = z?.ContactEntity?.Item?.PersonSSNIdentification?.IdentificationID?.Value,
                        PersonUSCitizenIndicator = z?.ContactEntity?.Item?.PersonUSCitizenIndicator?.Value,
                    })));

            var ContactEntities = new[] {
                AuthorizedRepresentatives,
                CareRecipients,
                PersonAssociations,
                ScreeningApplicants,
                ContactInformations?.Select(x => x?.Contacts)?.SelectManySafely(x => x)?.ToArray(),
                RSSOOLCI?.SelectManySafely(x => x)?.SelectManySafely(x => x)?.ToArray(),
                RSSOCI?.SelectManySafely(x => x)?.SelectManySafely(x => x)?.ToArray(),
                RSDOOLCI?.SelectManySafely(x => x)?.SelectManySafely(x => x)?.ToArray(),
                RSDOCI?.SelectManySafely(x => x)?.SelectManySafely(x => x)?.ToArray(),
                RISOOLCI?.SelectManySafely(x => x)?.SelectManySafely(x => x)?.ToArray(),
                RISOCI?.SelectManySafely(x => x)?.SelectManySafely(x => x)?.ToArray(),
                RIDOOLCI?.SelectManySafely(x => x)?.SelectManySafely(x => x)?.ToArray(),
                RIDOCI?.SelectManySafely(x => x)?.SelectManySafely(x => x)?.ToArray()
            };

            var ContactEntityResponse = context.SaveContactEntity(ContactEntities.SelectMany(x => x).ToArray());
            var ContactEntityResponses = ContactEntities.Select(x =>
                x.Select(y => ContactEntityResponse.Where(z => ContactEntityComparer.Comparer.Equals(z, y)).FirstOrDefault()).ToArray()).ToArray();

            var AuthorizedRepresentativeResponse = ContactEntityResponses[0];
            var CareRecipientResponse = ContactEntityResponses[1];
            var PersonAssociationResponse = ContactEntityResponses[2];
            var ScreeningApplicantResponse = ContactEntityResponses[3];
            var ContactInformationResponse = ContactEntityResponses[4];
            var RSSOOLCIResponse = ContactEntityResponses[5];
            var RSSOCIResponse = ContactEntityResponses[6];
            var RSDOOLCIResponse = ContactEntityResponses[7];
            var RSDOCIResponse = ContactEntityResponses[8];
            var RISOOLCIResponse = ContactEntityResponses[9];
            var RISOCIResponse = ContactEntityResponses[10];
            var RIDOOLCIResponse = ContactEntityResponses[11];
            var RIDOCIResponse = ContactEntityResponses[12];

            //  End of ContactEntity persisting

            // ReferralsSent -> SourceOrganization
            var RSSO = screenings
                ?.Select(x => x?.ReferralsSent)
                ?.SelectManySafely(x => x)
                ?.Select(a => new Organization
                {
                    OrganizationBranchName = a?.SourceOrganization?.OrganizationBranchName?.Value,
                    OrganizationCode = a?.SourceOrganization?.OrganizationCode?.Value.ToSafeString(), // is Enum
                    OrganizationIdentification = a?.SourceOrganization?.OrganizationIdentification?.IdentificationID?.Value,
                    OrganizationName = a?.SourceOrganization?.OrganizationName?.Value,
                    OrganizationUnitName = a?.SourceOrganization?.OrganizationUnitName?.Value
                });

            var RSSOResponse = context.SaveOrganization(RSSO?.ToArray());

            // ReferralsSent -> SourceOrganization -> OrganizationLocation -> LocationContactInformation
            var RSSOOLCISkipTakeLengths = RSSOOLCI?.Select(x => x?.Select(y => new[] { 0, y?.Count() ?? 0 }))
                ?.SelectManySafely(x => x)?.SelectManySafely(x => x);

            // ReferralsSent -> SourceOrganization -> OrganizationLocation
            var RSSOOL = screenings?.Select(x =>
                x?.ReferralsSent?.Zip(RSSOResponse, (a, b) =>
                    a?.SourceOrganization?.OrganizationLocation?.Select(z => new Location
                    {
                        OrganizationID = b.OrganizationID,
                        LocationName = z?.LocationName?.Value,
                        LatitudeDegreeValue = z?.Item?.GeographicCoordinateLatitude?.LatitudeDegreeValue?.Value,
                        LatitudeMinuteValue = z?.Item?.GeographicCoordinateLatitude?.LatitudeMinuteValue?.Value,
                        LatitudeSecondValue = z?.Item?.GeographicCoordinateLatitude?.LatitudeSecondValue?.Value,
                        LongitudeDegreeValue = z?.Item?.GeographicCoordinateLongitude?.LongitudeDegreeValue?.Value,
                        LongitudeMinuteValue = z?.Item?.GeographicCoordinateLongitude?.LongitudeMinuteValue?.Value,
                        LongitudeSecondValue = z?.Item?.GeographicCoordinateLongitude?.LongitudeSecondValue?.Value,
                        LocationIdentification = z?.LocationIdentification?.IdentificationID?.Value,
                    })
                    ?.Where(z => !string.IsNullOrWhiteSpace(z?.LocationName))
                    ))
                    ?.SelectManySafely(x => x)?.SelectManySafely(x => x)?.ToArray();

            var RSSOOLResponse = context.SaveLocation(RSSOOL);

            // ReferralsSent -> SourceOrganization -> OrganizationPrimaryContactInformation
            var RSSOCISkipTakeLengths = RSSOCI?.Select(x => x?.Select(y => new[] { 0, y?.Count() ?? 0 }))
                ?.SelectManySafely(x => x)?.SelectManySafely(x => x);

            // ReferralsSent -> DestinationOrganization
            var RSDO = screenings
                ?.Select(x => x?.ReferralsSent)
                ?.SelectManySafely(x => x)
                ?.Select(a => new Organization
                {
                    OrganizationBranchName = a?.DestinationOrganization?.OrganizationBranchName?.Value,
                    OrganizationCode = a?.DestinationOrganization?.OrganizationCode?.Value.ToSafeString(), // is Enum
                    OrganizationIdentification = a?.DestinationOrganization?.OrganizationIdentification?.IdentificationID?.Value,
                    OrganizationName = a?.DestinationOrganization?.OrganizationName?.Value,
                    OrganizationUnitName = a?.DestinationOrganization?.OrganizationUnitName?.Value
                });

            var RSDOResponse = context.SaveOrganization(RSDO?.ToArray());

            // ReferralsSent -> DestinationOrganization -> OrganizationLocation -> LocationContactInformation
            var RSDOOLCISkipTakeLengths = RSDOOLCI?.Select(x => x?.Select(y => new[] { 0, y?.Count() ?? 0 }))
                ?.SelectManySafely(x => x)?.SelectManySafely(x => x);

            // ReferralsSent -> DestinationOrganization -> OrganizationLocation
            var RSDOOL = screenings?.Select(x =>
                x?.ReferralsSent?.Zip(RSDOResponse, (a, b) =>
                    a?.DestinationOrganization?.OrganizationLocation?.Select(z => new Location
                    {
                        OrganizationID = b.OrganizationID,
                        LocationName = z?.LocationName?.Value,
                        LatitudeDegreeValue = z?.Item?.GeographicCoordinateLatitude?.LatitudeDegreeValue?.Value,
                        LatitudeMinuteValue = z?.Item?.GeographicCoordinateLatitude?.LatitudeMinuteValue?.Value,
                        LatitudeSecondValue = z?.Item?.GeographicCoordinateLatitude?.LatitudeSecondValue?.Value,
                        LongitudeDegreeValue = z?.Item?.GeographicCoordinateLongitude?.LongitudeDegreeValue?.Value,
                        LongitudeMinuteValue = z?.Item?.GeographicCoordinateLongitude?.LongitudeMinuteValue?.Value,
                        LongitudeSecondValue = z?.Item?.GeographicCoordinateLongitude?.LongitudeSecondValue?.Value,
                        LocationIdentification = z?.LocationIdentification?.IdentificationID?.Value,
                    })
                    ?.Where(z => !string.IsNullOrWhiteSpace(z?.LocationName))
                    ))
                    ?.SelectManySafely(x => x)?.SelectManySafely(x => x)?.ToArray();

            var RSDOOLResponse = context.SaveLocation(RSDOOL);

            // ReferralsSent -> DestinationOrganization -> OrganizationPrimaryContactInformation
            var RSDOCISkipTakeLengths = RSDOCI?.Select(x => x?.Select(y => new[] { 0, y?.Count() ?? 0 }))
                ?.SelectManySafely(x => x)?.SelectManySafely(x => x);

            // ReferralsSent
            var RSScreeningIDs = screenings?.Select(x => x?.ReferralsSent?.Select(y => x?.ScreeningID?.IdentificationID?.Value))?.SelectManySafely(x => x);

            var RS = screenings?.Select(x => x?.ReferralsSent)
                ?.SelectManySafely(x => x)
                ?.Zip(RSSOResponse, (a, b) => new
                {
                    Screening = a,
                    SourceOrganizationID = b?.OrganizationID
                })
                ?.Zip(RSDOResponse, (a, b) => new
                {
                    a?.Screening,
                    a?.SourceOrganizationID,
                    DestinationOrganizationID = b?.OrganizationID
                })
                ?.Zip(RSScreeningIDs, (a, b) => new
                {
                    a?.Screening,
                    a?.SourceOrganizationID,
                    a?.DestinationOrganizationID,
                    ScreeningID = b
                })
                ?.Select(x => new Referral
                {
                    DestinationOrganizationID = x?.DestinationOrganizationID,
                    ReferralBasisForOutcome = x?.Screening?.ReferralBasisForOutcome?.Value,
                    ReferralCreatedBy = x?.Screening?.ReferralCreatedBy?.Value,
                    ReferralCreatedDate = x?.Screening?.ReferralCreatedDate?.Value,
                    ReferralID = x?.Screening?.ReferralID?.IdentificationID?.Value,
                    ReferralGenerationCode = x?.Screening?.ReferralGenerationCode?.Value.ToSafeString(),
                    ReferralPriorityCode = x?.Screening?.ReferralPriorityCode?.Value.ToSafeString(),
                    ReferralReason = x?.Screening?.ReferralReason?.Value,
                    ReferralReasonCode = x?.Screening?.ReferralReasonCode?.Value.ToSafeString(),
                    ReferralStatusCode = x?.Screening?.ReferralStatusCode?.Value.ToSafeString(),
                    ReferralType = "S", // Sent
                    ReferralUpdatedBy = x?.Screening?.ReferralUpdatedBy?.Value,
                    ReferralUpdatedDate = x?.Screening?.ReferralUpdatedDate?.Value,
                    ScreeningID = x?.ScreeningID,
                    SourceOrganizationID = x?.SourceOrganizationID
                })
                ?.ToArray();

            context.AddRange<Referral>(RS);

            // ReferralsIdentified -> SourceOrganization
            var RISO = screenings
                ?.Select(x => x?.ReferralsIdentified)
                ?.SelectManySafely(x => x)
                ?.Select(a => new Organization
                {
                    OrganizationBranchName = a?.SourceOrganization?.OrganizationBranchName?.Value,
                    OrganizationCode = a?.SourceOrganization?.OrganizationCode?.Value.ToSafeString(), // is Enum
                    OrganizationIdentification = a?.SourceOrganization?.OrganizationIdentification?.IdentificationID?.Value,
                    OrganizationName = a?.SourceOrganization?.OrganizationName?.Value,
                    OrganizationUnitName = a?.SourceOrganization?.OrganizationUnitName?.Value
                });

            var RISOResponse = context.SaveOrganization(RISO?.ToArray());

            // ReferralsIdentified -> SourceOrganization -> OrganizationLocation -> LocationContactInformation

            var RISOOLCISkipTakeLengths = RISOOLCI?.Select(x => x?.Select(y => new[] { 0, y?.Count() ?? 0 }))
                ?.SelectManySafely(x => x)?.SelectManySafely(x => x);

            // ReferralsIdentified -> SourceOrganization -> OrganizationLocation
            var RISOOL = screenings?.Select(x =>
                x?.ReferralsIdentified?.Zip(RISOResponse, (a, b) =>
                    a?.SourceOrganization?.OrganizationLocation?.Select(z => new Location
                    {
                        OrganizationID = b.OrganizationID,
                        LocationName = z?.LocationName?.Value,
                        LatitudeDegreeValue = z?.Item?.GeographicCoordinateLatitude?.LatitudeDegreeValue?.Value,
                        LatitudeMinuteValue = z?.Item?.GeographicCoordinateLatitude?.LatitudeMinuteValue?.Value,
                        LatitudeSecondValue = z?.Item?.GeographicCoordinateLatitude?.LatitudeSecondValue?.Value,
                        LongitudeDegreeValue = z?.Item?.GeographicCoordinateLongitude?.LongitudeDegreeValue?.Value,
                        LongitudeMinuteValue = z?.Item?.GeographicCoordinateLongitude?.LongitudeMinuteValue?.Value,
                        LongitudeSecondValue = z?.Item?.GeographicCoordinateLongitude?.LongitudeSecondValue?.Value,
                        LocationIdentification = z?.LocationIdentification?.IdentificationID?.Value,
                    })
                    ?.Where(z => !string.IsNullOrWhiteSpace(z?.LocationName))
                    ))
                    ?.SelectManySafely(x => x)?.SelectManySafely(x => x)?.ToArray();

            var RISOOLResponse = context.SaveLocation(RISOOL);

            // ReferralsIdentified -> SourceOrganization -> OrganizationPrimaryContactInformation
            var RISOCISkipTakeLengths = RISOCI?.Select(x => x?.Select(y => new[] { 0, y?.Count() ?? 0 }))
                ?.SelectManySafely(x => x)?.SelectManySafely(x => x);

            // ReferralsIdentified -> DestinationOrganization
            var RIDO = screenings
                ?.Select(x => x?.ReferralsIdentified)
                ?.SelectManySafely(x => x)
                ?.Select(a => new Organization
                {
                    OrganizationBranchName = a?.DestinationOrganization?.OrganizationBranchName?.Value,
                    OrganizationCode = a?.DestinationOrganization?.OrganizationCode?.Value.ToSafeString(), // is Enum
                    OrganizationIdentification = a?.DestinationOrganization?.OrganizationIdentification?.IdentificationID?.Value,
                    OrganizationName = a?.DestinationOrganization?.OrganizationName?.Value,
                    OrganizationUnitName = a?.DestinationOrganization?.OrganizationUnitName?.Value
                });

            var RIDOResponse = context.SaveOrganization(RIDO?.ToArray());

            // ReferralsIdentified -> DestinationOrganization -> OrganizationLocation -> LocationContactInformation
            var RIDOOLCISkipTakeLengths = RIDOOLCI?.Select(x => x?.Select(y => new[] { 0, y?.Count() ?? 0 }))
                ?.SelectManySafely(x => x)?.SelectManySafely(x => x);

            // ReferralsIdentified -> DestinationOrganization -> OrganizationLocation
            var RIDOOL = screenings?.Select(x =>
                x?.ReferralsIdentified?.Zip(RIDOResponse, (a, b) =>
                    a?.DestinationOrganization?.OrganizationLocation?.Select(z => new Location
                    {
                        OrganizationID = b.OrganizationID,
                        LocationName = z?.LocationName?.Value,
                        LatitudeDegreeValue = z?.Item?.GeographicCoordinateLatitude?.LatitudeDegreeValue?.Value,
                        LatitudeMinuteValue = z?.Item?.GeographicCoordinateLatitude?.LatitudeMinuteValue?.Value,
                        LatitudeSecondValue = z?.Item?.GeographicCoordinateLatitude?.LatitudeSecondValue?.Value,
                        LongitudeDegreeValue = z?.Item?.GeographicCoordinateLongitude?.LongitudeDegreeValue?.Value,
                        LongitudeMinuteValue = z?.Item?.GeographicCoordinateLongitude?.LongitudeMinuteValue?.Value,
                        LongitudeSecondValue = z?.Item?.GeographicCoordinateLongitude?.LongitudeSecondValue?.Value,
                        LocationIdentification = z?.LocationIdentification?.IdentificationID?.Value,
                    })
                    ?.Where(z => !string.IsNullOrWhiteSpace(z?.LocationName))
                    ))
                    ?.SelectManySafely(x => x)?.SelectManySafely(x => x)?.ToArray();

            var RIDOOLResponse = context.SaveLocation(RIDOOL);

            // ReferralsIdentified -> DestinationOrganization -> OrganizationPrimaryContactInformation
            var RIDOCISkipTakeLengths = RIDOCI?.Select(x => x?.Select(y => new[] { 0, y?.Count() ?? 0 }))
                ?.SelectManySafely(x => x)?.SelectManySafely(x => x);

            // ReferralsIdentified
            var RIScreeningIDs = screenings?.Select(x => x?.ReferralsIdentified?.Select(y => x?.ScreeningID?.IdentificationID?.Value))?.SelectManySafely(x => x);

            var RI = screenings?.Select(x => x?.ReferralsIdentified)
                ?.SelectManySafely(x => x)
                ?.Zip(RISOResponse, (a, b) => new
                {
                    Screening = a,
                    SourceOrganizationID = b?.OrganizationID
                })
                ?.Zip(RIDOResponse, (a, b) => new
                {
                    a?.Screening,
                    a?.SourceOrganizationID,
                    DestinationOrganizationID = b?.OrganizationID
                })
                ?.Zip(RIScreeningIDs, (a, b) => new
                {
                    a?.Screening,
                    a?.SourceOrganizationID,
                    a?.DestinationOrganizationID,
                    ScreeningID = b
                })
                ?.Select(x => new Referral
                {
                    DestinationOrganizationID = x?.DestinationOrganizationID,
                    ReferralBasisForOutcome = x?.Screening?.ReferralBasisForOutcome?.Value,
                    ReferralCreatedBy = x?.Screening?.ReferralCreatedBy?.Value,
                    ReferralCreatedDate = x?.Screening?.ReferralCreatedDate?.Value,
                    ReferralID = x?.Screening?.ReferralID?.IdentificationID?.Value,
                    ReferralGenerationCode = x?.Screening?.ReferralGenerationCode?.Value.ToSafeString(),
                    ReferralPriorityCode = x?.Screening?.ReferralPriorityCode?.Value.ToSafeString(),
                    ReferralReason = x?.Screening?.ReferralReason?.Value,
                    ReferralReasonCode = x?.Screening?.ReferralReasonCode?.Value.ToSafeString(),
                    ReferralStatusCode = x?.Screening?.ReferralStatusCode?.Value.ToSafeString(),
                    ReferralType = "S", // Sent
                    ReferralUpdatedBy = x?.Screening?.ReferralUpdatedBy?.Value,
                    ReferralUpdatedDate = x?.Screening?.ReferralUpdatedDate?.Value,
                    ScreeningID = x?.ScreeningID,
                    SourceOrganizationID = x?.SourceOrganizationID
                })
                ?.ToArray();

            context.AddRange<Referral>(RI);

            // EntityContactEntities
            var ContactInformationScreeningIDs = screenings?.Select(x => x?.ScreeningApplicant?.ContactInformation?.Select(y => x?.ScreeningID?.IdentificationID?.Value))?.SelectManySafely(x => x);

            var ScreeningContactEntities = AuthorizedRepresentativeResponse?.Zip(Screenings, (a, b) => new EntityContactEntity
            {
                ContactEntityID = a?.ContactEntityID ?? 0,
                ContactEntityType = ContactEntityType.AR.ToSafeString(),
                EntityID = b?.ScreeningID
            })
            ?.Concat(CareRecipientResponse?.Zip(Screenings, (a, b) => new EntityContactEntity
            {
                ContactEntityID = a?.ContactEntityID ?? 0,
                ContactEntityType = ContactEntityType.CR.ToSafeString(),
                EntityID = b?.ScreeningID
            }))
            ?.Concat(PersonAssociationResponse?.Zip(Screenings, (a, b) => new EntityContactEntity
            {
                ContactEntityID = a?.ContactEntityID ?? 0,
                ContactEntityType = ContactEntityType.PA.ToSafeString(),
                EntityID = b?.ScreeningID
            }))
            ?.Concat(ScreeningApplicantResponse?.Zip(Screenings, (a, b) => new EntityContactEntity
            {
                ContactEntityID = a?.ContactEntityID ?? 0,
                ContactEntityType = ContactEntityType.SA.ToSafeString(),
                EntityID = b?.ScreeningID
            }))
            ?.Concat(ContactInformationResponse?.Zip(ContactInformationScreeningIDs, (a, b) => new EntityContactEntity
            {
                ContactEntityID = a?.ContactEntityID ?? 0,
                ContactEntityType = ContactEntityType.CI.ToSafeString(),
                EntityID = b
            }))
            ?.ToArray();

            var RSSOOLContactEntities = RSSOOLCIResponse?.Paginate(RSSOOLCISkipTakeLengths)?.Zip(RSSOOLResponse, (a, b) => a?.Select(y => new EntityContactEntity
            {
                ContactEntityID = y?.ContactEntityID ?? 0,
                ContactEntityType = ContactEntityType.LO.ToSafeString(),
                EntityID = b?.LocationID.ToSafeString()
            }))
            ?.SelectManySafely(x => x)
            ?.ToArray();

            var RSSOContactEntities = RSSOCIResponse?.Paginate(RSSOCISkipTakeLengths)?.Zip(RSSOResponse, (a, b) => a?.Select(y => new EntityContactEntity
            {
                ContactEntityID = y?.ContactEntityID ?? 0,
                ContactEntityType = ContactEntityType.OR.ToSafeString(),
                EntityID = b?.OrganizationID.ToSafeString()
            }))
            ?.SelectManySafely(x => x)
            ?.ToArray();

            var RSDOOLContactEntities = RSDOOLCIResponse?.Paginate(RSDOOLCISkipTakeLengths)?.Zip(RSDOOLResponse, (a, b) => a?.Select(y => new EntityContactEntity
            {
                ContactEntityID = y?.ContactEntityID ?? 0,
                ContactEntityType = ContactEntityType.LO.ToSafeString(),
                EntityID = b?.LocationID.ToSafeString()
            }))
            ?.SelectManySafely(x => x)
            ?.ToArray();

            var RSDOContactEntities = RSDOCIResponse?.Paginate(RSDOCISkipTakeLengths)?.Zip(RSDOResponse, (a, b) => a?.Select(y => new EntityContactEntity
            {
                ContactEntityID = y?.ContactEntityID ?? 0,
                ContactEntityType = ContactEntityType.OR.ToSafeString(),
                EntityID = b?.OrganizationID.ToSafeString()
            }))
            ?.SelectManySafely(x => x)
            ?.ToArray();

            var RISOOLContactEntities = RISOOLCIResponse?.Paginate(RISOOLCISkipTakeLengths)?.Zip(RISOOLResponse, (a, b) => a?.Select(y => new EntityContactEntity
            {
                ContactEntityID = y?.ContactEntityID ?? 0,
                ContactEntityType = ContactEntityType.LO.ToSafeString(),
                EntityID = b?.LocationID.ToSafeString()
            }))
            ?.SelectManySafely(x => x)
            ?.ToArray();

            var RISOContactEntities = RISOCIResponse?.Paginate(RISOCISkipTakeLengths)?.Zip(RISOResponse, (a, b) => a?.Select(y => new EntityContactEntity
            {
                ContactEntityID = y?.ContactEntityID ?? 0,
                ContactEntityType = ContactEntityType.OR.ToSafeString(),
                EntityID = b?.OrganizationID.ToSafeString()
            }))
            ?.SelectManySafely(x => x)
            ?.ToArray();

            var RIDOOLContactEntities = RIDOOLCIResponse?.Paginate(RIDOOLCISkipTakeLengths)?.Zip(RIDOOLResponse, (a, b) => a?.Select(y => new EntityContactEntity
            {
                ContactEntityID = y?.ContactEntityID ?? 0,
                ContactEntityType = ContactEntityType.LO.ToSafeString(),
                EntityID = b?.LocationID.ToSafeString()
            }))
            ?.SelectManySafely(x => x)
            ?.ToArray();

            var RIDOContactEntities = RIDOCIResponse?.Paginate(RIDOCISkipTakeLengths)?.Zip(RIDOResponse, (a, b) => a?.Select(y => new EntityContactEntity
            {
                ContactEntityID = y?.ContactEntityID ?? 0,
                ContactEntityType = ContactEntityType.OR.ToSafeString(),
                EntityID = b?.OrganizationID.ToSafeString()
            }))
            ?.SelectManySafely(x => x)
            ?.ToArray();

            context.AddEntityContactEntities(ScreeningContactEntities
                .Concat(RSSOOLContactEntities).Concat(RSSOContactEntities).Concat(RSDOOLContactEntities).Concat(RSDOContactEntities)
                .Concat(RISOOLContactEntities).Concat(RISOContactEntities).Concat(RIDOOLContactEntities).Concat(RIDOContactEntities).ToArray());
        }

        public Schema.SendReferralResponsePayloadType sendReferralRequest(Schema.SendReferralRequestPayloadType request)
        {
            request = this.GetRequest<Schema.SendReferralRequestPayloadType>("SendReferralRequest");
            var ReferralID = request?.ReferralID?.IdentificationID?.Value;

            Repository.SaveSendReferralRequest(request);

            ((Action)GetResponse.Request).FireAndForget();
            return new Schema.SendReferralResponsePayloadType();
        }
    }
}