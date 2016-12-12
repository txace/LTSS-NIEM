# LTSS-NIEM
This is an implementation of the Long-Term Services and Supports referral service.  The web service utilizes a NIEM 3.0 (National Information Exchange Model) schema.

### Requirements
- Visual Studio 2015+
- IIS 7.5+
- SQL Server 2008+

### Installation
- Configure keys containing ******** in the app.config.
- Generate referral database.
Within the package manager console run either
   ```sh
   update-database
   ```
   to directly generate the database or
   ```sh
   update-database -Script -SourceMigration:0
   ```
   to generate a create script for the database starting from the initial migration.
- Right-click on LTSSWebService in the Solution Explorer and choose to publish to a website hosted on either a local or remote IIS instance.  If you choose to publish to a local instance, simply copy the files to your desired remote IIS website folder.

### Workflow of the service
When a referral id is sent from LTSS, its associated SendReferralRequestPayloadType is persisted to a database common to the various TxACE centers.  After it is persisted, the service immediately responds with a referral acknowledgement and makes a retrieveReferralInfo request for the data associated with the referral id.  Once the data is received we persist it to the database and validate that it can be deserialized.  Then we call updateReferralInfo with "AC" (acknowledgement) if we successfully retrieved valid XML and "PF" (process failure) otherwise.

### How to update an existing installation
- Backup your production database.
- Run the following commands against your production database (possibly twice for constraint violations):
   ```sh
   delete ContactEntity
   delete Enrollment
   delete EntityContactEntity
   delete EntityEvent
   delete Location
   delete Organization
   delete Referral
   delete Screening

   update Request SET IsProcessed = 0
   ```
- Deploy the updated code making sure to preserve your existing app.config file.
- Assuming your app.config file is pointing to your production database, run
   ```sh
   update-database
   ```
   to make sure your database has the latest schema.

### To-do
- Send notifications (email or otherwise) to relevant parties when referrals are sent.
- Push desired referral data into center specific databases.
- Create useful forms and/or an application that enables centers to effectively use the referral data.