#ifndef F_CPU
#define F_CPU 16000000UL 
#endif

#include <avr/io.h>
#include <util/delay.h>
#include <stdio.h>

// ==========================================
// UART Functions
// ==========================================
void UART_Init(void) {
    UBRRL = 103; // 9600 Baud Rate
    UBRRH = 0;
    UCSRB = (1 << RXEN) | (1 << TXEN);
    UCSRC = (1 << URSEL) | (1 << UCSZ1) | (1 << UCSZ0);
}

void UART_SendChar(char data) {
    while (!(UCSRA & (1 << UDRE)));
    UDR = data;
}

void UART_SendString(char* str) {
    while (*str) UART_SendChar(*str++);
}

char UART_Receive(void) {
    // ????? ????? ?????? (???? ?????? ?? ?????)
    if (UCSRA & (1 << RXC)) return UDR;
    return 0; 
}

// ==========================================
// ADC (Temperature)
// ==========================================
void ADC_Init(void) {
    ADMUX = (1 << REFS0);
    ADCSRA = (1 << ADEN) | (1 << ADPS2) | (1 << ADPS1) | (1 << ADPS0);
}

uint16_t ADC_Read(uint8_t channel) {
    ADMUX = (ADMUX & 0xF8) | (channel & 0x07);
    ADCSRA |= (1 << ADSC);
    while (ADCSRA & (1 << ADSC));
    return ADC;
}

// ==========================================
// Ultrasonic (Protected)
// ==========================================
void Ultrasonic_Init(void) {
    DDRA |= (1 << PA4);  // Trig Output
    DDRA &= ~(1 << PA5); // Echo Input
}

uint16_t Get_Distance(void) {
    uint16_t count = 0;
    uint16_t timeout = 0; 
    
    PORTA &= ~(1 << PA4);
    _delay_us(2);
    PORTA |= (1 << PA4);
    _delay_us(10);
    PORTA &= ~(1 << PA4);
    
    while (!(PINA & (1 << PA5))) {
        timeout++;
        _delay_us(1);
        if (timeout > 30000) return 999; 
    }
    
    while (PINA & (1 << PA5)) {  
        count++;
        _delay_us(1);
        if (count > 30000) break; 
    }
    return (uint16_t)(count * 0.034 / 2); 
}

// ==========================================
// Motors (Simple Direction)
// ==========================================
void PWM_Init(void) {
    DDRD |= (1 << PD4) | (1 << PD5);
    TCCR1A = (1 << COM1A1) | (1 << COM1B1) | (1 << WGM10);
    TCCR1B = (1 << WGM12) | (1 << CS11) | (1 << CS10); // Prescaler 64
}

void Motor_Init(void) {
    DDRA |= 0x0F; // PA0 to PA3 as Output
    PORTA &= 0xF0; // ????? ???????? ???
}

// ==========================================
// Main Loop
// ==========================================
int main(void) {
    UART_Init();
    PWM_Init();
    Motor_Init();
    Ultrasonic_Init();
    ADC_Init();

    // ????? ???? ???????? ??? ???? ???? (255)
    OCR1A = 255; 
    OCR1B = 255; 

    char command;
    char buffer[32];
    uint16_t distance;
    uint16_t temp_c;

    while (1) {
        // 1. ????? ????????
        distance = Get_Distance();
        temp_c = (uint32_t)ADC_Read(6) * 500 / 1024; 
        
        // 2. ????? ?????? ???????? ???? ??????? ????????
        sprintf(buffer, "D:%d,T:%d#", distance, temp_c);
        UART_SendString(buffer);

        // 3. ?????? ???????? (???????)
        if (distance > 0 && distance < 15) {
            PORTA &= 0xF0; // ????? ???????? ?? ?????? ??? ????????
        }

        // 4. ??????? ????? ?????? ??????????
        command = UART_Receive(); 
        if (command != 0) {
            // ?????? (PORTA & 0xF0) ?????? ??? ???? ???????? ?? ?????
            switch (command) {
                case 'F': PORTA = (PORTA & 0xF0) | 0b00000101; break; // ????
                case 'B': PORTA = (PORTA & 0xF0) | 0b00001010; break; // ???
                case 'R': PORTA = (PORTA & 0xF0) | 0b00000110; break; // ????
                case 'L': PORTA = (PORTA & 0xF0) | 0b00001001; break; // ????
                case 'S': PORTA = (PORTA & 0xF0) | 0x00;       break; // ????
            }
        }
        
        _delay_ms(50);
    }
    return 0;
}