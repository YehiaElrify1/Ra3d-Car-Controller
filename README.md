# 🏎️ Ra3d Car Controller (رعــد)

![.NET MAUI](https://img.shields.io/badge/.NET_MAUI-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Bluetooth](https://img.shields.io/badge/Bluetooth_Classic-0082FC?style=for-the-badge&logo=bluetooth&logoColor=white)
![Android](https://img.shields.io/badge/Android-3DDC84?style=for-the-badge&logo=android&logoColor=white)

An intelligent, high-performance cross-platform mobile application built to control, monitor, and interact with a custom-built smart car (Ra3d). The app establishes a robust Serial Port Profile (SPP) connection via Bluetooth Classic to communicate with an ATmega32 microcontroller.

## ✨ Key Features

* **Real-Time Bluetooth Radar:** Scans for nearby discoverable Bluetooth Classic devices (specifically HC-05/06 modules) with seamless Android permission handling.
* **Global State Management:** Implements an Event-Driven Architecture (Publisher-Subscriber pattern) to instantly broadcast hardware connection state changes across all UI components.
* **Neon-Themed UI:** A visually striking, dark-mode-first dashboard with glowing neon accents, optimized for automotive telemetry and control.
* **Adaptive Assets:** Utilizes MAUI's `Resizetizer` for crisp, resolution-independent SVG icons and adaptive app launchers.
* **Asynchronous Networking:** Non-blocking socket connections ensuring the UI remains smooth and responsive during hardware handshakes.

## 🛠️ Hardware Ecosystem

This application is the software brain for the **Ra3d** hardware system, which includes:
* **Microcontroller:** ATmega32 (Handling motor logic, UART parsing, and hardware interrupts).
* **Communication:** HC-05 Bluetooth Module (UART to Bluetooth SPP bridge).
* **Actuators:** Motor Drivers and DC Motors.

## 📱 Screenshots

> *(Add your screenshots here by dragging and dropping images directly into this README file on GitHub)*
> 
> | Setup & Radar | Drive Dashboard | Analytics |
> | :---: | :---: | :---: |
> | ![Setup Placeholder](https://via.placeholder.com/250x500.png?text=Setup+Page) | ![Drive Placeholder](https://via.placeholder.com/250x500.png?text=Drive+Page) | ![Analytics Placeholder](https://via.placeholder.com/250x500.png?text=Analytics+Page) |

## ⚙️ Software Architecture & Tech Stack

* **Framework:** .NET MAUI (.NET 10)
* **Language:** C# 12 / XAML
* **Bluetooth Library:** `InTheHand.Net.Bluetooth` (32feet.net)
* **Architecture:** Clean UI separation, leveraging global event actions for state propagation without memory leaks.

## 🚀 Getting Started

### Prerequisites
1. [.NET 10 SDK](https://dotnet.microsoft.com/download) installed.
2. Visual Studio 2022 / VS Code with MAUI extension.
3. An Android device running API 21+ (Physical device required for Bluetooth testing).
