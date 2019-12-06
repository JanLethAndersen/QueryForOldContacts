# QueryForOldContacts

## Formål:
Koden har til formål at fremsøge records i *contacts*-entiteten, som opfylder bestemte kriterier. Disse resultater returneres til CRM via en action, som kaldes fra et HTTP-request. Det gør det ultimativt muligt at sekretæren at fremsøge gamle og lukkede konti, og genåbne dem.

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
1. Sekretæren åbner CRM og trykker på "Genansættelse" på Brugeradministrations dashboarded. Dette åbner formularen *Bruger formular: Genansættelse*.
2. Sekretæren indtaster *brugernavn* eller *fornavn*, *efternavn* og *fødselsdato* på den lukkede konto, hun ønsker at genåbne.
3. Sekretæren trykker på "Søg efter eksisterende konto".
   1. Web resourcen *sdu_lookforoldcontacts.js* kaldes, og der udføres et HTTP POST Request til actionen *ass_getOldContact*. Der medsendes her de oplysninger som hun har indtastet.
   2. Actionen kalder dette Custom Workflow, som baseret på inputsne fremsøger et match i *contacts*-entiteten.
   3. Såfremt der er ét match, og kun ét match, så udfyldes output parametrene fra denne kode, som i actionen gemmes i variabler. Disse variabler er tilgængelige i HTTP-requestets result, og værdierne indsættes i det pågældende felter på formularen.
4. Sekretæren udfylder de resterende felter efter behov, og trykker "Fortsæt".
5. En email afsendes til hhv. Servicedesk og sekretæren selv - og kontoen er hermed bestilt genåbnet.

## findOldContacts.cs
Koden består af metoden *searchForRecord* udover den abstrakte metode *execute*.

Koden eksekveres som følgende:
1. Input parametrene hentes, og det sikres at der ikke videreføres en null-værdi, da søgning vil fejle.
2. En QueryExpression skabes, hvori følgende regler opsættes:
   1. Brugernavn eller fornavn, efternavn og fødselsdato skal være udfyldt.
   2. Contacten skal være inaktiv eller skal CRM udløbet være maksimalt 13 måneder gammelt og samtidigt være før dags dato.
3. QueryExpressionen anvendes i et *RetrieveMultiple*, hvor output parametrene udfyldes, såfremt der kun et ét resultat.
	1. Værdierne skal på formularen ende ud i en blanding af tekst, look-up, dato og option-set værdier - hvorfor der fra værdi til værdi foretages forskellige ting. 
 
### Metoden searchForRecord:
Metoden *searchForRecord* kaldes, hvis der ønskes et ID retuneret for at udfylde feltet på formularen. Dette er nødvendigt når et look-up skal udfyldes, ved eksempelvis omkostningsstedet.
Metoden kaldes med to KeyValuePair<string,string>, som fodrer en QueryExpression med hvilke felte der skal matche.
Årsagen til at det er nødvendigt at søge i record på denne måde, er at det værdi som returneres fra den fremsøgede contact, ikke stemmer overens med den værdi der skal udfyldes på brugeradministration ifbm. genåbning.
Omkostningsstedet på contacten er eksempelvis ikke den samme entiteten der anvendes på brugeradministration, grundet sikkerhedshensyn.
Det er derfor nødvendigt at udtrække omkostningstedet fra den fremsøgede contact og anvende dette ID til at fremsøge en record i entiteten brugeradm omk.sted (som er relateret til omkostningsstedet).



     

