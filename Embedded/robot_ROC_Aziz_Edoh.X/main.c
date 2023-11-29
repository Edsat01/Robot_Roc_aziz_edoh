#include <stdio.h>
#include <stdlib.h>
#include <xc.h>
#include "ChipConfig.h"
#include "IO.h"
#include "PWM.h"
#include "timer.h"
int main(void) {
    /***************************************************************************************************/
    //Initialisation de l?oscillateur
    /****************************************************************************************************/
    InitOscillator();
    /****************************************************************************************************/
    // Configuration des entr�es sorties
    /****************************************************************************************************/
    InitIO();

    InitTimer23();
    InitTimer1();
    
    InitPWM();
   // PWMSetSpeed(-20, MOTEUR_DROIT);
   // PWMSetSpeed(-20, MOTEUR_GAUCHE);
   
    PWMUpdateSpeed();
    LED_BLANCHE = 1;
    LED_BLEUE = 1;
    LED_ORANGE = 1;

    /****************************************************************************************************/
    // Boucle Principale
    /****************************************************************************************************/
    while (1) {
     
    } // fin main
}