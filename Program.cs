//
// F. Chaxel 2009
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace ModbusConsole
{
    class Program
    {
        
        /// <summary>
        ///    Envoi de la requete de demande d'�criture d'un mot 
        ///    Provoque une exception en cas de probl�me sur le port
        ///    � l'appelant de mettre en place un try catch autour de l'appel
        /// </summary>
        /// <param name="Sockcli">Objet TCP Client</param>
        /// <param name="AddMot">Adresse Mot � ecrire (en HEX)</param>
        /// <param name="Valeur">Valeur � ecrire</param>
        static void WriteTcpWordRequest(TcpClient Sockcli, short AddMot, short Valeur)
        {
            // tableau de taille 15
            byte[] Trame = new byte[12];

            // Identifiant
            Trame[0] = 0;
            Trame[1] = 0;

            Trame[2] = 0;
            Trame[3] = 0;
            Trame[4] = 0;

            // Longueur
            Trame[5] = 6;

            // Numero de l'esclave
            Trame[6] = 0;

            // Code fonction: 06 Ecriture d'un mot
            Trame[7] = 6;

            // Adresse mot � �crire
            Trame[8] = (byte)((AddMot >> 8) & 0xff); // MSB
            Trame[9] = (byte)(AddMot & 0xff); // LSB

            // Valeur
            Trame[10] = (byte)((Valeur >> 8) & 0xff); // MSB
            Trame[11] = (byte)(Valeur & 0xff); // LSB

            
            try
            {
                // Emission avec TimeOut de 100ms
                Sockcli.SendTimeout = 100;
                Sockcli.Client.Send(Trame);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut)
                    Console.WriteLine("Time out.");
                else
                    // on re-balance l'erreur � l'appelant ... c'est pas notre probl�me ici
                    throw e;
            }

        }

        static void ReadTcpWordRequest(TcpClient Sockcli, short AddMot)
        {

            byte[] Trame = new byte[12];
            
            // Identifiant de la requ�te
            Trame[0] = 0;
            Trame[1] = 0;

            Trame[2] = 0;
            Trame[3] = 0;
            Trame[4] = 0;

            // Longueur
            Trame[5] = 6;

            // Num�ro de l'esclave
            Trame[6] = 0;

            // Code Fonction: 03 Lecture d'un mot
            Trame[7] = 03;

            // Adresse mot � lire
            Trame[8] = (byte)((AddMot >> 8) & 0xff); // MSB
            Trame[9] = (byte)(AddMot & 0xff); // LSB

            // Mot � lire: 1 mot
            Trame[10] = 0;
            Trame[11] = 1;

            // Emission avec TimeOut de 100ms
            Sockcli.SendTimeout = 100;
            Sockcli.Client.Send(Trame);

        }

        // R�cup�ration de la reponse en provenance de l'�quipement
        // analyse de la trame
        // retourne false si rien re�u ou r�ception d'un truc sans int�ret
        // retourne true si tout est OK
        // Provoque une exception en cas de probl�me sur le port
        //    � l'appelant de mettre en place un try catch autour de l'appel
        static bool ReadTcpWordConfirm(TcpClient Sockcli, out short Valeurlue)
        {

            Valeurlue = 0;
            
            try
            {

                // Reception avec attente de la r�ponse
                byte[] TrameRecp = new byte[40];
                Sockcli.Client.ReceiveTimeout = 2000;	// serait une bonne id�e de param�trer cela !
                int NbOct=Sockcli.Client.Receive(TrameRecp);

                if (NbOct == 11)    // La trame a t'elle la bonne taille
                {
                    // Code fonction 3 ou 4, 2 octets en retour
                    if (((TrameRecp[7] == 3) || (TrameRecp[7] == 4)) && (TrameRecp[8] == 2))
                    {
                        Valeurlue = (short)(TrameRecp[9] * 256 + TrameRecp[10]);
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            catch (SocketException e)
            {
                // Pas de r�ponse dans le delai imparti
                if (e.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut)
                    return false;
                else
                    // on re-balance l'erreur � l'appelant ... c'est pas notre probl�me ici
                    throw e;

            }

        }

        // R�cup�ration de la reponse en provenance de l'�quipement
        // analyse de la trame
        // retourne false si rien re�u ou r�ception d'un truc sans int�ret
        // retourne true si tout est OK
        // Provoque une exception en cas de probl�me sur le port
        //    � l'appelant de mettre en place un try catch autour de l'appel
        static bool WriteTcpWordConfirm(TcpClient Sockcli)
        {
            try
            {
                // Reception avec attente de la r�ponse
                var TrameRecp = new byte[40];
                Sockcli.Client.ReceiveTimeout = 2000;	// serait une bonne id�e de param�trer cela !
                int NbOct = Sockcli.Client.Receive(TrameRecp);

                if (NbOct > 0)    // La trame a t'elle la bonne taille
                {
                    // Code fonction 3 ou 4, 2 octets en retour
                    if ((TrameRecp[7] == 6) && (TrameRecp[8] == 2))
                    {
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;

            }
            catch (SocketException e)
            {

                // Pas de r�ponse dans le delai imparti
                if (e.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut)
                    return false;
                else
                    // on re-balance l'erreur � l'appelant ... c'est pas notre probl�me ici
                    throw e;
            }
        }

        static void Main(string[] args)
        {
           

            TcpClient SockClient; 

            // Connexion au Serveur 
            // try catch � mettre en place
            SockClient = new TcpClient(AddressFamily.InterNetwork);	// IP Version 4
            SockClient.Connect("194.199.51.34", 502);


            //ReadTcpWordRequest(SockClient, 3);
            WriteTcpWordRequest(SockClient, 0x0202, 0);
            //short Val;
            //ReadTcpWordConfirm(SockClient, out Val);
            var test = WriteTcpWordConfirm(SockClient);

            Console.WriteLine(test);

            SockClient.Close();
            Console.ReadKey();
        }

    }
}
