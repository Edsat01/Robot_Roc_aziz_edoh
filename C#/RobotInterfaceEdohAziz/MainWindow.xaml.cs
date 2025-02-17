﻿using System;
using System.Collections.Generic;
using System.Linq;
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
using ExtendedSerialPort_NS;
using System.IO.Ports;
using System.Windows.Threading;
using System.Diagnostics.Tracing;

namespace RobotInterface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ExtendedSerialPort serialPort1;
        bool isRoyalBlue;
        
        Robot robot = new Robot();      // Instanciation de la classe robot

        public MainWindow()
        {
            InitializeComponent();
           serialPort1 = new ExtendedSerialPort("COM21", 115200, Parity.None, 8, StopBits.One); // Initialize the serial port
         //   serialPort1 = new ReliableSerialPort("COM21", 115200, Parity.None, 8, StopBits.One);

            serialPort1.DataReceived += SerialPort1_DataReceived; 

            serialPort1.Open();                                                                 // Open the serial port


            timerAffichage = new DispatcherTimer();
            timerAffichage.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timerAffichage.Tick += TimerAffichage_Tick;
            timerAffichage.Start();


        }

        //string robot.receivedText = "";

        private void TimerAffichage_Tick(object? sender, EventArgs e)
        {
            while (robot.byteListReceived.Count >0)
            {
                var c = robot.byteListReceived.Dequeue();
                textBoxReception.Text += "0x" + c.ToString("X2") + " ";
                //textBoxReception.Text += Encoding.ASCII.GetString(robot.byteListReceived.ToArray());
                

            }
        }
        public void SerialPort1_DataReceived(object sender, DataReceivedArgs e)
        {
           
            robot.receivedText += Encoding.UTF8.GetString(e.Data, 0, e.Data.Length);
            //textBoxReception.Text += receivedText;
            foreach(byte value in e.Data)
            {
                robot.byteListReceived.Enqueue(value);
            }
        }

        DispatcherTimer timerAffichage;


        byte CalculateChecksum(int msgFunction, int msgPayloadLength, byte[] msgPayload)
        {
            
            byte checksum = 0;
            checksum ^= 0xFE;
            checksum ^= (byte)(msgFunction >> 8);
            checksum ^= (byte)(msgFunction >> 0);
            checksum ^= (byte)(msgPayloadLength >> 8);
            checksum ^= (byte)(msgPayloadLength >> 0);
            for (int i = 0; i < msgPayloadLength; i++)
            {
                checksum ^= msgPayload[i];
            }
            return checksum;
        }

        void UartEncodeAndSendMessage(int msgFunction, int msgPayloadLength, byte[] msgPayload)
        {
            var checksum = CalculateChecksum(msgFunction, msgPayloadLength, msgPayload);
            byte[] messageComplet = new byte[msgPayloadLength + 6];
            int pos = 0;
            messageComplet[pos++] = 0xFE;
            messageComplet[pos++] = (byte)(msgFunction >> 8);
            messageComplet[pos++] = (byte)(msgFunction >> 0);
            messageComplet[pos++] = (byte)(msgPayloadLength >> 8);
            messageComplet[pos++] = (byte)(msgPayloadLength >> 0);
            for (int i = 0;i < msgPayloadLength;i++)
            {
                messageComplet[pos++] = msgPayload[i];
            }
            messageComplet[pos++] = checksum;
            serialPort1.Write(messageComplet, 0, msgPayloadLength+6);
        }
        public enum StateReception
        {
            Waiting,
            FunctionMSB,
            FunctionLSB,
            PayloadLengthMSB,
            PayloadLengthLSB,
            Payload,
            CheckSum
        }
        StateReception rcvState = StateReception.Waiting;
        int msgDecodedFunction = 0;
        int msgDecodedPayloadLength = 0;
        byte[] msgDecodedPayload;
        int msgDecodedPayloadIndex = 0;
        private void DecodeMessage(byte c)
        {
            switch (rcvState)
            {
                case StateReception.Waiting:
                    if (c== 0XFE){
                        rcvState = StateReception.FunctionMSB;
                       
                    }
                    break;
                case StateReception.FunctionMSB:
                    msgDecodedFunction = (c >> 8);
                    rcvState = StateReception.FunctionLSB;
                break;
                case StateReception.FunctionLSB:
                   msgDecodedFunction |= c ;
                    rcvState = StateReception.PayloadLengthMSB;
                break;
                case StateReception.PayloadLengthMSB:
                    msgDecodedPayloadLength = (c << 8);
                    rcvState = StateReception.PayloadLengthLSB;
                break;
                case StateReception.PayloadLengthLSB:
                    msgDecodedPayloadLength |= c ;
                    if (msgDecodedPayloadLength == 0)
                    {
                        rcvState = StateReception.CheckSum;
                    }
                    else
                    {
                        msgDecodedPayload = new byte[msgDecodedPayloadLength];
                        msgDecodedPayloadIndex = 0;
                        rcvState = StateReception.Payload;
                    }
                break;
                case StateReception.Payload:
                    msgDecodedPayload[msgDecodedPayloadIndex] = c;
                    msgDecodedPayloadIndex++;
                    if(msgDecodedPayloadIndex >= msgDecodedPayloadLength)
                        rcvState = StateReception.CheckSum;
                    break;
                case StateReception.CheckSum:
                    var receivedChecksum = c;
                    byte calculatedChecksum = CalculateChecksum(msgDecodedFunction, msgDecodedPayloadLength, msgDecodedPayload) ;
                    if (calculatedChecksum == receivedChecksum)
                    {
                        //Success, on a un message valide
                        Console.WriteLine("message reçu");
                    }

                    else
                    {
                        throw new Exception();

                    }
                    rcvState = StateReception.Waiting;
                break;
                default:
                    rcvState = StateReception.Waiting;
                    break;
            }
        }

        void transmissiontexte(int msgfunction, int msgPayloadLength, byte[] msgPayload)
        {
            var encodemessage = "";
            
            encodemessage = UartEncodeAndSendMessage(0x0080, msgPayloadLength, Encoding.ASCII.GetBytes(msgPayload));         
        }

        private string UartEncodeAndSendMessage(int v, int msgPayloadLength, object value)
        {
            throw new NotImplementedException();
        }

        private void ButtonEnvoyer_Click(object sender, RoutedEventArgs e)
        {

            if (isRoyalBlue) {

                ButtonEnvoyer.Background = Brushes.Beige;
                isRoyalBlue= false;
            }
            else {
                ButtonEnvoyer.Background = Brushes.RoyalBlue;
                isRoyalBlue = true;
            }
           
            SendMessage();
           
        }
        private void SendMessage()
        {
            //serialPort1.WriteLine(textBoxEmission.Text);
            //textBoxReception.Text += "Reçu : " + textBoxEmission.Text + "\n";
            //textBoxEmission.Text = "";*
            UartEncodeAndSendMessage(0x0080, 7, Encoding.ASCII.GetBytes("Bonjour"));


        }

        private void TextBoxEmission_Keyup(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                
                SendMessage();
                
            }

        }

        // Evenement click du bouton clear
        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            if (ButtonClear.IsEnabled) { textBoxReception.Text = ""; }
        }


        // Evenement click du bouton Test
        private void ButtonTest_Click(object sender, RoutedEventArgs e)
        {
          
            byte[] byteList = new byte[20];
            for (int i = 0; i < 20; i++)
                byteList[i] = (byte)(i * 2);

            //UartEncodeAndSendMessage(0x0080, byteList.Length, byteList);
            //serialPort1.Write(byteList, 0, byteList.Length);
            //UartEncodeAndSendMessage(0x0080, 7, Encoding.ASCII.GetBytes("Bonjour"));


        }
    }
}
