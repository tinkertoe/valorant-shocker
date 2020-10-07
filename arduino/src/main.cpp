#include <Arduino.h>
#include <ESP8266WiFi.h>

int shock_pin = 4;
char shock_awnser = 0;

int r = 0;

void setup()
{
    wifi_set_opmode(NULL_MODE);
    pinMode(shock_pin, OUTPUT);
    Serial.begin(115200);
}

void loop()
{
    digitalWrite(shock_pin, LOW);
    if (Serial.available() > 0)
    {
        r = Serial.read() - '0';
        Serial.println(r);

        if (r >= 0 && r <= 9)
        {
            Serial.println("shocking...");
            digitalWrite(shock_pin, HIGH);
            delay((r + 1) * 10);
            digitalWrite(shock_pin, LOW);
        }
    }
    digitalWrite(shock_pin, LOW);
}