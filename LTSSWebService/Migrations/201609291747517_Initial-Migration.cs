namespace LTSSWebService.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class InitialMigration : DbMigration
    {
        public override void Down()
        {
            DropForeignKey("dbo.EntityContactEntity", "ContactEntityID", "dbo.ContactEntity");
            DropForeignKey("dbo.Organization", "ContactEntity_ContactEntityID", "dbo.ContactEntity");
            DropForeignKey("dbo.Location", "ContactEntity_ContactEntityID", "dbo.ContactEntity");
            DropForeignKey("dbo.Organization", "LocationID", "dbo.Location");
            DropForeignKey("dbo.Referral", "SourceOrganizationID", "dbo.Organization");
            DropForeignKey("dbo.Referral", "DestinationOrganizationID", "dbo.Organization");
            DropForeignKey("dbo.Referral", "ScreeningID", "dbo.Screening");
            DropForeignKey("dbo.Enrollment", "ScreeningID", "dbo.Screening");
            DropIndex("dbo.EntityContactEntity", new[] { "ContactEntityID" });
            DropIndex("dbo.Enrollment", new[] { "ScreeningID" });
            DropIndex("dbo.Referral", new[] { "SourceOrganizationID" });
            DropIndex("dbo.Referral", new[] { "ScreeningID" });
            DropIndex("dbo.Referral", new[] { "DestinationOrganizationID" });
            DropIndex("dbo.Organization", new[] { "ContactEntity_ContactEntityID" });
            DropIndex("dbo.Organization", new[] { "LocationID" });
            DropIndex("dbo.Location", new[] { "ContactEntity_ContactEntityID" });
            DropTable("dbo.Request");
            DropTable("dbo.EntityEvent");
            DropTable("dbo.EntityContactEntity");
            DropTable("dbo.Enrollment");
            DropTable("dbo.Screening");
            DropTable("dbo.Referral");
            DropTable("dbo.Organization");
            DropTable("dbo.Location");
            DropTable("dbo.ContactEntity");
        }

        public override void Up()
        {
            CreateTable(
                "dbo.ContactEntity",
                c => new
                {
                    ContactEntityID = c.Int(nullable: false, identity: true),
                    CategoryCode = c.String(),
                    ContactEmailID = c.String(),
                    ContactMailingAddress = c.String(),
                    ContactTelephoneNumber = c.String(),
                    ContactWebsiteURI = c.String(),
                    EthnicityCode = c.String(),
                    PersonAgeMeasure = c.String(),
                    PersonBirthDate = c.String(),
                    PersonFullName = c.String(),
                    PersonGivenName = c.String(),
                    PersonLivingIndicator = c.Boolean(),
                    PersonMiddleName = c.String(),
                    PersonNamePrefixText = c.String(),
                    PersonNameSuffixText = c.String(),
                    PersonSSNIdentification = c.String(),
                    PersonSurName = c.String(),
                    PersonSurNamePrefixText = c.String(),
                    PersonUSCitizenIndicator = c.Boolean(),
                    RACCode = c.String(),
                    SEXCode = c.String(),
                })
                .PrimaryKey(t => t.ContactEntityID);

            CreateTable(
                "dbo.Location",
                c => new
                {
                    LocationID = c.Int(nullable: false, identity: true),
                    LatitudeDegreeValue = c.Decimal(precision: 18, scale: 2),
                    LatitudeMinuteValue = c.Decimal(precision: 18, scale: 2),
                    LatitudeSecondValue = c.Decimal(precision: 18, scale: 2),
                    LocationIdentification = c.String(),
                    LocationName = c.String(),
                    LongitudeDegreeValue = c.Decimal(precision: 18, scale: 2),
                    LongitudeMinuteValue = c.Decimal(precision: 18, scale: 2),
                    LongitudeSecondValue = c.Decimal(precision: 18, scale: 2),
                    ContactEntity_ContactEntityID = c.Int(),
                })
                .PrimaryKey(t => t.LocationID)
                .ForeignKey("dbo.ContactEntity", t => t.ContactEntity_ContactEntityID)
                .Index(t => t.ContactEntity_ContactEntityID);

            CreateTable(
                "dbo.Organization",
                c => new
                {
                    OrganizationID = c.Int(nullable: false, identity: true),
                    LocationID = c.Int(nullable: false),
                    OrganizationBranchName = c.String(),
                    OrganizationCode = c.String(),
                    OrganizationIdentification = c.String(),
                    OrganizationName = c.String(),
                    OrganizationUnitName = c.String(),
                    ContactEntity_ContactEntityID = c.Int(),
                })
                .PrimaryKey(t => t.OrganizationID)
                .ForeignKey("dbo.Location", t => t.LocationID, cascadeDelete: true)
                .ForeignKey("dbo.ContactEntity", t => t.ContactEntity_ContactEntityID)
                .Index(t => t.LocationID)
                .Index(t => t.ContactEntity_ContactEntityID);

            CreateTable(
                "dbo.Referral",
                c => new
                {
                    ReferralID = c.String(nullable: false, maxLength: 128),
                    DestinationOrganizationID = c.Int(nullable: false),
                    ReferralBasisForOutcome = c.String(),
                    ReferralCreatedBy = c.String(),
                    ReferralCreatedDate = c.DateTime(),
                    ReferralGenerationCode = c.String(),
                    ReferralPriorityCode = c.String(),
                    ReferralReason = c.String(),
                    ReferralReasonCode = c.String(),
                    ReferralStatusCode = c.String(),
                    ReferralType = c.String(),
                    ReferralUpdatedBy = c.String(),
                    ReferralUpdatedDate = c.DateTime(),
                    ScreeningID = c.String(nullable: false, maxLength: 128),
                    SourceOrganizationID = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.ReferralID)
                .ForeignKey("dbo.Screening", t => t.ScreeningID, cascadeDelete: true)
                .ForeignKey("dbo.Organization", t => t.DestinationOrganizationID)
                .ForeignKey("dbo.Organization", t => t.SourceOrganizationID)
                .Index(t => t.DestinationOrganizationID)
                .Index(t => t.ScreeningID)
                .Index(t => t.SourceOrganizationID);

            CreateTable(
                "dbo.Screening",
                c => new
                {
                    ScreeningID = c.String(nullable: false, maxLength: 128),
                    AplicantInterestedInMDCPIndicator = c.Boolean(),
                    ApplicantAutismOrPervasiveDisorderIndicator = c.Boolean(),
                    ApplicantCaregiverForExistingConditionsIndicator = c.Boolean(),
                    ApplicantCaregiverNeedsInformationIndicator = c.String(),
                    ApplicantCognitiveImpairmentIndicator = c.Boolean(),
                    ApplicantDeafAndBlindIndicator = c.Boolean(),
                    ApplicantDevelopmentalDisabilityIndicator = c.Boolean(),
                    ApplicantDroppedActivitiesAndInterestsIndicator = c.String(),
                    ApplicantFeelsFullOfEnergyIndicator = c.String(),
                    ApplicantFeelsLifeIsEmptyIndicator = c.String(),
                    ApplicantGetsCareFromPersonWhoNeedsHelpIndicator = c.String(),
                    ApplicantGetsCareIndicator = c.Boolean(),
                    ApplicantHasConsentToReleaseOfInformationIndicator = c.Boolean(),
                    ApplicantHasConsumedAlcoholInPast30DaysIndicator = c.Boolean(),
                    ApplicantHasDiabetesMedicationInPast30DaysIndicator = c.Boolean(),
                    ApplicantHasMedicationForAnxietyInPast30DaysIndicator = c.Boolean(),
                    ApplicantHasMedicationForHeartDiseasesOrBloodPressureInPast30DaysIndicator = c.Boolean(),
                    ApplicantHasMedicationForSleepInPast30DaysIndicator = c.Boolean(),
                    ApplicantHasNarcoticsPrescriptionInPast30DaysIndicator = c.Boolean(),
                    ApplicantHasQuestionsAboutMedicareOrLTCInsuranceIndicator = c.String(),
                    ApplicantHasTriCareInsurance = c.Boolean(),
                    ApplicantIntellectualDisabilityIndicator = c.Boolean(),
                    ApplicantInterestedInCLASSIndicator = c.Boolean(),
                    ApplicantInterestedInPACEIndicator = c.Boolean(),
                    ApplicantInterestedInSTARPLUSIndicator = c.Boolean(),
                    ApplicantInterestednDBMDIndicator = c.Boolean(),
                    ApplicantLikelyhoodOfMovingToNursingFacilityCode = c.String(),
                    ApplicantMentalHealthDiagnosisIndicator = c.Boolean(),
                    ApplicantNeedsAssistanceCallingForHelpIndicator = c.Boolean(),
                    ApplicantNumberOfInstancesOfAlcoholConsumptionInPastOneYearCode = c.String(),
                    ApplicantPhysicalDisabilityIndicator = c.Boolean(),
                    ApplicantRequiresAssistanceMeetingPersonalNeedsIndicator = c.Boolean(),
                    ApplicantRequiresLicensedNurseSupervisionIndicator = c.Boolean(),
                    ApplicantScreeningObservations = c.String(),
                    ApplicantSubstanceAbuseIndicator = c.Boolean(),
                    ApplicantThinksWonderfulToBeAliveNowIndicator = c.String(),
                    ApplicationStatusCode = c.String(),
                    AssociationDateRangeEndDate = c.String(),
                    AssociationDateRangeStartDate = c.String(),
                    AssociationDescriptionText = c.String(),
                    CareRecipientAssociationDateRangeEndDate = c.String(),
                    CareRecipientAssociationDateRangeStartDate = c.String(),
                    CareRecipientAssociationDescriptionText = c.String(),
                    CareRecipientFamilyRelationshipCode = c.String(),
                    CareRecipientHasCognitiveImpairment = c.Boolean(),
                    CareRecipientHasOtherDisabilities = c.Boolean(),
                    CareRecipientLivesWithCaregiver = c.String(),
                    CareRecipientLocationCountyCode = c.String(),
                    CareRecipientLocationStateUSPostalServiceCode = c.String(),
                    CareRecipientPersonCaretakerDependentCode = c.String(),
                    Comments = c.String(),
                    CreateDateTime = c.DateTime(),
                    CreatedBy = c.String(),
                    CurrentInterestListPersonCsilID = c.String(),
                    DataEnteredBy = c.String(),
                    EnteredDateTime = c.DateTime(),
                    FamilyRelationshipCode = c.String(),
                    HasNoFixedAddressIndicator = c.String(),
                    LivesOutsideStateTemporarilyIndicator = c.String(),
                    MedicaidInformationDateTime = c.DateTime(),
                    MedicaidInformationDateTime1 = c.DateTime(),
                    PersonCaretakerDependentCode = c.String(),
                    PersonCmbhsID = c.String(),
                    PersonCsilID = c.String(),
                    PersonLtssID = c.String(),
                    PersonMarriedIndicator = c.Boolean(),
                    PersonMedicaidID = c.String(),
                    PersonMedicareID = c.String(),
                    PersonPreferredLanguage = c.String(),
                    PersonTiersID = c.String(),
                    PersonUSVeteranIndicator = c.Boolean(),
                    ReferringOrganization = c.String(),
                    ScreeningApplicantComments = c.String(),
                    ScreeningQuestionnaireVersionIndicator = c.String(),
                    TermsOfUseExpiryDate = c.DateTime(),
                    TermsOfUseExternalID = c.String(),
                    TermsOfUseOrganizationID = c.String(),
                    TermsOfUseSignatureDate = c.DateTime(),
                    TermsOfUseSignedBy = c.String(),
                    TermsOfUseStaffName = c.String(),
                    TermsOfUseTypeCode = c.String(),
                    TermsOfUseVersionIndicator = c.String(),
                    TypeOfMedicalInsurance = c.String(),
                    UpdateDateTime = c.DateTime(),
                    UpdatedBy = c.String(),
                    USNaturalizedCitizenIndicator = c.Boolean(),
                })
                .PrimaryKey(t => t.ScreeningID);

            CreateTable(
                "dbo.Enrollment",
                c => new
                {
                    EnrollmentID = c.Int(nullable: false, identity: true),
                    EndDate = c.String(),
                    IdentificationID = c.String(),
                    PlanName = c.String(),
                    ProgramName = c.String(),
                    ScreeningID = c.String(nullable: false, maxLength: 128),
                    StartDate = c.String(),
                })
                .PrimaryKey(t => t.EnrollmentID)
                .ForeignKey("dbo.Screening", t => t.ScreeningID, cascadeDelete: true)
                .Index(t => t.ScreeningID);

            CreateTable(
                "dbo.EntityContactEntity",
                c => new
                {
                    EntityID = c.String(nullable: false, maxLength: 128),
                    ContactEntityID = c.Int(nullable: false),
                    ContactEntityType = c.String(nullable: false, maxLength: 128),
                })
                .PrimaryKey(t => new { t.EntityID, t.ContactEntityID, t.ContactEntityType })
                .ForeignKey("dbo.ContactEntity", t => t.ContactEntityID, cascadeDelete: true)
                .Index(t => t.ContactEntityID);

            CreateTable(
                "dbo.EntityEvent",
                c => new
                {
                    EntityEventID = c.Int(nullable: false, identity: true),
                    Diff = c.String(),
                    EnittyID = c.Int(),
                    EntityType = c.Int(),
                    RequestID = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.EntityEventID);

            CreateTable(
                "dbo.Request",
                c => new
                {
                    RequestID = c.Int(nullable: false, identity: true),
                    Exception = c.String(),
                    IsProcessed = c.Boolean(nullable: false),
                    RequestData = c.String(),
                    RequestDate = c.DateTime(nullable: false),
                    ResponseData = c.String(),
                    ServiceType = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.RequestID);
        }
    }
}