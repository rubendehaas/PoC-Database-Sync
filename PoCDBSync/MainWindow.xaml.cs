using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;


namespace PoCDBSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private SqlConnection clientConn;
        private SqlConnection serverConn;
        

        public MainWindow()
        {
            
            InitializeComponent();
            
            //Set server and client connectionStrings
            setConnection();

            //Check server request status
            CheckConnectivity();

            //Provision the database
            //setServer();
            //SetClient();
        }



        private Boolean CheckConnectivity()
        {
            try
            {
                lblQuestionOffline.Content = OfflineQuery();

                //Setup a request to the server
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://www.google.com");
                request.Timeout = 5000;
                request.Credentials = CredentialCache.DefaultNetworkCredentials;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                //Check whether you can connect to the server
                if (response.StatusCode.Equals(HttpStatusCode.OK))
                {
                    //Color the status
                    SyncStatus.Fill = new SolidColorBrush(Color.FromRgb(0,255,0));
                    btnSyncData.IsEnabled = true;
                    
                    lblQuestion.Content = OnlineQuery();
                    
                    return true;
                }
                else
                {
                    SyncStatus.Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    btnSyncData.IsEnabled = false;
                    return false;
                }
            }
            catch
            {
                SyncStatus.Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                btnSyncData.IsEnabled = false;
                return false;
            }
        }

        private void SyncLocalToServer()
        {
            this.Sync();

            lblQuestion.Content = OnlineQuery();
            lblQuestionOffline.Content = OfflineQuery();
        }

        private void btnSyncData_Click(object sender, RoutedEventArgs e)
        {
            SyncLocalToServer();
        }

        public void setConnection()
        {
            clientConn = new SqlConnection(ConfigurationManager.ConnectionStrings["PoCDBSync.Properties.Settings.OfflineDatabaseConnectionString"].ConnectionString);

            // create a connection to the server database
            serverConn = new SqlConnection(ConfigurationManager.ConnectionStrings["PoCDBSync.Properties.Settings.OnlineDatabaseConnectionString"].ConnectionString);
        }
        
        //TODO: run one time
        public void SetClient()
        {

            // get the description of SyncScope from the server database
            DbSyncScopeDescription scopeDesc = SqlSyncDescriptionBuilder.GetDescriptionForScope("MySyncScope", serverConn);

            // create server provisioning object based on the SyncScope
            SqlSyncScopeProvisioning clientProvision = new SqlSyncScopeProvisioning(clientConn, scopeDesc);

            // starts the provisioning process
            clientProvision.Apply();

            Console.WriteLine("Client Successfully Provisioned.");
            Console.ReadLine();
        }


        //TODO: run for all required tables
        public void setServer()
        {

            // define a new scope named MySyncScope
            DbSyncScopeDescription scopeDesc = new DbSyncScopeDescription("MySyncScope");

            // get the description of the CUSTOMER & PRODUCT table from SERVER database
            DbSyncTableDescription questionsTableDesc = SqlSyncDescriptionBuilder.GetDescriptionForTable("questions", serverConn);
            DbSyncTableDescription answersTableDesc = SqlSyncDescriptionBuilder.GetDescriptionForTable("answers", serverConn);

            // add the table description to the sync scope definition
            scopeDesc.Tables.Add(questionsTableDesc);
            scopeDesc.Tables.Add(answersTableDesc);

            // create a server scope provisioning object based on the MySyncScope
            SqlSyncScopeProvisioning serverProvision = new SqlSyncScopeProvisioning(serverConn, scopeDesc);

            // skipping the creation of table since table already exists on server
            serverProvision.SetCreateTableDefault(DbSyncCreationOption.Skip);

            // start the provisioning process
            serverProvision.Apply();

            Console.WriteLine("Server Successfully Provisioned.");
            Console.ReadLine();
        }

        private void Sync()
        {
            

            // create the sync orhcestrator
            SyncOrchestrator syncOrchestrator = new SyncOrchestrator();

            // set local provider of orchestrator to a sync provider associated with the 
            // MySyncScope in the client database
            syncOrchestrator.LocalProvider = new SqlSyncProvider("MySyncScope", clientConn);

            // set the remote provider of orchestrator to a server sync provider associated with
            // the MySyncScope in the server database
            syncOrchestrator.RemoteProvider = new SqlSyncProvider("MySyncScope", serverConn);

            // set the direction of sync session to Upload and Download
            syncOrchestrator.Direction = SyncDirectionOrder.DownloadAndUpload;

            // subscribe for errors that occur when applying changes to the client
            ((SqlSyncProvider)syncOrchestrator.LocalProvider).ApplyChangeFailed += new EventHandler<DbApplyChangeFailedEventArgs>(Program_ApplyChangeFailed);

            // execute the synchronization process
            SyncOperationStatistics syncStats = syncOrchestrator.Synchronize();

            // print statistics
            Console.WriteLine("Start Time: " + syncStats.SyncStartTime);
            Console.WriteLine("Total Changes Uploaded: " + syncStats.UploadChangesTotal);
            Console.WriteLine("Total Changes Downloaded: " + syncStats.DownloadChangesTotal);
            Console.WriteLine("Complete Time: " + syncStats.SyncEndTime);
            Console.WriteLine(String.Empty);
            Console.ReadLine();

        }

        static void Program_ApplyChangeFailed(object sender, DbApplyChangeFailedEventArgs e)
        {
            // display conflict type
            Console.WriteLine(e.Conflict.Type);

            if (e.Conflict.Type == DbConflictType.LocalUpdateRemoteUpdate)
            {
                Console.WriteLine("diadeem");
            }

                // display error message 
                Console.WriteLine(e.Error);
        }

        private String OnlineQuery()
        {
            string query = "SELECT question FROM questions WHERE id='C346EC15-2DCA-46AD-A69A-08499EF3C92B'";
            string output = "";

            SqlCommand sqlCmd = new SqlCommand(query, serverConn);
            serverConn.Open();

            using (SqlDataReader oReader = sqlCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    output = oReader["question"].ToString();
                }

                serverConn.Close();
            }
            return output;
        }

        private String OfflineQuery()
        {
            string query = "SELECT question FROM questions WHERE id='c346ec15-2dca-46ad-a69a-08499ef3c92b'";
            string output = "";

            SqlCommand sqlCmd = new SqlCommand(query, clientConn);
            clientConn.Open();

            using (SqlDataReader oReader = sqlCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    output = oReader["question"].ToString();
                }

                clientConn.Close();
            }
            return output;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string query = "UPDATE questions SET question=@QUESTION WHERE id='C346EC15-2DCA-46AD-A69A-08499EF3C92B'";

            clientConn.Open();
            SqlCommand sqlCmd = new SqlCommand(query, clientConn);
            sqlCmd.Parameters.AddWithValue("@QUESTION", tbClient2.Text);
            sqlCmd.ExecuteNonQuery();
            clientConn.Close();

            lblQuestionOffline.Content = OfflineQuery();
        }

        private void btnSave1_Click(object sender, RoutedEventArgs e)
        {
            
            string query = "UPDATE questions SET question=@QUESTION WHERE id='C346EC15-2DCA-46AD-A69A-08499EF3C92B'";

            serverConn.Open();
            SqlCommand sqlCmd = new SqlCommand(query, serverConn);
            sqlCmd.Parameters.AddWithValue("@QUESTION", tbClient1.Text);
            sqlCmd.ExecuteNonQuery();
            serverConn.Close();

            lblQuestion.Content = OnlineQuery();
        }

        private void btnInsertOnline_Click(object sender, RoutedEventArgs e)
        {
            string query = "INSERT into questions(question) VALUES(@QUESTION)";

            serverConn.Open();
            SqlCommand sqlCmd = new SqlCommand(query, serverConn);
            sqlCmd.Parameters.AddWithValue("@QUESTION", tbClient1.Text);
            sqlCmd.ExecuteNonQuery();
            serverConn.Close();

            lblQuestion.Content = OnlineQuery();
        }

        private void btnInsertOnline1_Click(object sender, RoutedEventArgs e)
        {
            string query = "INSERT into questions(question) VALUES(@QUESTION)";

            clientConn.Open();
            SqlCommand sqlCmd = new SqlCommand(query, clientConn);
            sqlCmd.Parameters.AddWithValue("@QUESTION", tbClient2.Text);
            sqlCmd.ExecuteNonQuery();
            clientConn.Close();

            lblQuestion.Content = OnlineQuery();
        }
    }
}
