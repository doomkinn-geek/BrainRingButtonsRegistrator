# BrainRingButtonsRegistrator

Программа для захвата нажатых кнопок с устройства, которое передаёт данные по RS-232 порту

Протокол обмена с устройством:

BRRING PC-to-HW COMMUNICATION PROTOCOL

PC to HW messages

| ASCII char  | Description |
| ------------- | ------------- |
|1	          | MAX registrations for pressed buttons = 1|
| Content Cell  | Content Cell  |
	
R	          Start new round
1	          MAX registrations for pressed buttons = 1
2	          MAX registrations for pressed buttons = 2
3	          MAX registrations for pressed buttons = 3 (default)
4	          MAX registrations for pressed buttons = 4
5	          MAX registrations for pressed buttons = 5
6	          MAX registrations for pressed buttons = 6
7	          MAX registrations for pressed buttons = 7
8	          MAX registrations for pressed buttons = 8


PC to HW messages
ASCII char	Description
E	          Error: buttons are not released at start of round
1	          Pressed button 1
2         	Pressed button 2
3         	Pressed button 3
4	          Pressed button 4
5	          Pressed button 5
6	          Pressed button 6
7	          Pressed button 7
8	          Pressed button 8

