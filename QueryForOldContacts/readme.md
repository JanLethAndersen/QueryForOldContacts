# QueryForOldContacts

## Form�l:
Koden har til form�l at frems�ge records i *contacts*-entiteten, som opfylder bestemte kriterier. Disse resultater returneres til CRM via en action, som kaldes fra et HTTP-request. Det g�r det ultimativt muligt at sekret�ren at frems�ge gamle og lukkede konti, og gen�bne dem.

## Parametre:
### Input:
* string username
* DateTime birthdate
* string firstname
* string lastname

### Output:
* string contactid
* string fullnameOutput 
* string omkStedId
* string domaene
* string emailDomaene
* string lokation
* string stillingsBetegnelse
* DateTime birthDateOutput
* string usernameOutput
 
## Flow:
1. Sekret�ren �bner CRM og trykker p� "Genans�ttelse" p� Brugeradministrations dashboarded. Dette �bner formularen *Bruger formular: Genans�ttelse*.
2. Sekret�ren indtaster *brugernavn* eller *fornavn*, *efternavn* og *f�dselsdato* p� den lukkede konto, hun �nsker at gen�bne.
3. Sekret�ren trykker p� "S�g efter eksisterende konto".
   1. Web resourcen *sdu_lookforoldcontacts.js* kaldes, og der udf�res et HTTP POST Request til actionen *ass_getOldContact*. Der medsendes her de oplysninger som hun har indtastet.
   2. Actionen kalder dette Custom Workflow, som baseret p� inputsne frems�ger et match i *contacts*-entiteten.
   3. S�fremt der er �t match, og kun �t match, s� udfyldes output parametrene fra denne kode, som i actionen gemmes i variabler. Disse variabler er tilg�ngelige i HTTP-requestets result, og v�rdierne inds�ttes i det p�g�ldende felter p� formularen.
4. Sekret�ren udfylder de resterende felter efter behov, og trykker "Forts�t".
5. En email afsendes til hhv. Servicedesk og sekret�ren selv - og kontoen er hermed bestilt gen�bnet.

## findOldContacts.cs
Koden best�r af metoden *searchForRecord* udover den abstrakte metode *execute*.

Koden eksekveres som f�lgende:
1. Input parametrene hentes, og det sikres at der ikke videref�res en null-v�rdi, da s�gning vil fejle.
2. En QueryExpression skabes, hvori f�lgende regler ops�ttes:
   1. Brugernavn eller fornavn, efternavn og f�dselsdato skal v�re udfyldt.
   2. Contacten skal v�re inaktiv eller skal CRM udl�bet v�re maksimalt 13 m�neder gammelt og samtidigt v�re f�r dags dato.
3. QueryExpressionen anvendes i et *RetrieveMultiple*, hvor output parametrene udfyldes, s�fremt der kun et �t resultat.
	1. V�rdierne skal p� formularen ende ud i en blanding af tekst, look-up, dato og option-set v�rdier - hvorfor der fra v�rdi til v�rdi foretages forskellige ting. 
 
### Metoden searchForRecord:
Metoden *searchForRecord* kaldes, hvis der �nskes et ID retuneret for at udfylde feltet p� formularen. Dette er n�dvendigt n�r et look-up skal udfyldes, ved eksempelvis omkostningsstedet.
Metoden kaldes med to KeyValuePair<string,string>, som fodrer en QueryExpression med hvilke felte der skal matche.
�rsagen til at det er n�dvendigt at s�ge i record p� denne m�de, er at det v�rdi som returneres fra den frems�gede contact, ikke stemmer overens med den v�rdi der skal udfyldes p� brugeradministration ifbm. gen�bning.
Omkostningsstedet p� contacten er eksempelvis ikke den samme entiteten der anvendes p� brugeradministration, grundet sikkerhedshensyn.
Det er derfor n�dvendigt at udtr�kke omkostningstedet fra den frems�gede contact og anvende dette ID til at frems�ge en record i entiteten brugeradm omk.sted (som er relateret til omkostningsstedet).



     

