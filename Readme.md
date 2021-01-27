# Założenia:  
1.Serwer wielowątkowy oparty na socketach  
2.Infroamcje szyfrowane kluczem AES(Póki co nie spełnione)  
3.urzadzenia loguja sie wczesniej zdefiniowanymi identyfikatorami.(Możliwe ponowne zalogowanie po rozłączeniu)  
4. Komunikacja za pomocą ramek w standardzie: |ilość znaków(0-255)|Typ Wiadomosci|ilość odbiorców|Nicki(6znaków, kodowanie ASCII)|Dane  
5. Gdy ktoś próbuje połączyć się pod juz zarejestrowany nick serwer wysyła bajt 0 w celu sprawdzenia połączenia, powinien on zostać zignorowany. Jesli polaczenie jest nieaktywne nowy użytkownik przypisywany jest pod nick.  
# Komendy:
- Register_Rpi = 0x11,
- Register_desktop = 0x22,
- SendToRpi= 0x33,
- SendToDesktop=0x44
