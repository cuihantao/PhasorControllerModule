#include "pmu.h"

#pragma pack(1)

struct cfg1a {
  uint16_t sync;         // sync byte + frame type and v num
  uint16_t framesize;    // num bytes in frame 6.2 (including chk)
  uint16_t id;           // stream id num 6.2
  uint32_t soc;          // second of century 6.2
  uint8_t fracsec_b;     // msg time quality
  uint8_t fracsec_a[3];  // frac of second
  uint32_t time_base;    // resolution of fracsec ts
  uint16_t num_pmu;      // number of pmus
  char stn[16];          // station name
  } cfg1a;

struct cfg1b { //one per pmu
  uint16_t id_code;      // data source id number
  uint16_t format;       // data format within data frame
  uint16_t phnmr;        // num phasors
  uint16_t annmr;        // num analog vals
  uint16_t dgnmr;        // num digital vals
} cfg1b;

struct cfg1c {
  uint16_t data_rate;    // data rate
  uint16_t checksum;
  
} crg1c;

void swap(char *p, int len)
{
  int i;
  char tmp;
  for(i = 0; i < len/2; i++)
    {
      tmp = p[len-i-1];
      p[len-i-1] = p[i];
      p[i] = tmp;
    }
}

void cfg1()
{
  //printf("%d\n",sizeof(cfg1a));
  //printf("%d\n",sizeof(cfg1b));
  //printf("%d\n",sizeof(uint16_t));
  
  void *cfg1 = malloc(65535); // get largest size
  void *cfg1r = malloc(65535); //reversed big endian
  // read from file and fill in p
  FILE * fp;
  char * line = NULL;
  size_t len = 0;
  ssize_t read;

  fp = fopen("./config/cfg", "r");
  if (fp == NULL)
    exit(EXIT_FAILURE);

  struct cfg1a *pmua,*pmuar;
  pmua = cfg1;
  pmuar = cfg1r;
  // read first part of conf frame

  printf("starting reading");
  
  if(read = getline(&line, &len, fp) != -1)
    {
      pmua->sync=(uint16_t)atoi(line); 
      pmuar->sync=(uint16_t)atoi(line);
      swap((char*)&(pmuar->sync),sizeof(uint16_t)); 
    }
  if(read = getline(&line, &len, fp) != -1)
    {
      pmua->id=(uint16_t)atoi(line); 
      pmuar->id=(uint16_t)atoi(line);
      swap((char*)&(pmuar->id),sizeof(uint16_t)); 
    }
  
  if(read = getline(&line, &len, fp) != -1)
    {
      pmua->fracsec_b=(uint8_t)atoi(line);
      pmuar->fracsec_b=(uint8_t)atoi(line);
      swap((char*)&(pmuar->fracsec_b),sizeof(uint8_t));
    }

  if(read = getline(&line, &len, fp) != -1)
    {
      pmua->time_base=(uint32_t)atoi(line); 
      pmuar->time_base=(uint32_t)atoi(line);
      swap((char*)&(pmuar->time_base),sizeof(uint32_t));
    }

  if(read = getline(&line, &len, fp) != -1)
    {
      pmua->num_pmu=(uint16_t)atoi(line); 
      pmuar->num_pmu=(uint16_t)atoi(line);
      swap((char*)&(pmuar->num_pmu),sizeof(uint16_t)); 
    }
  if(read = getline(&line, &len, fp) != -1)
    {
      strncpy ( pmua->stn,line,16); 
      strncpy (pmuar->stn,line,16);
      //swap((char*)&(pmuar->stn),16);
    }

  
  //middle loop part
  //printf("size of cfg1b %d\n", sizeof(struct cfg1b));

  struct cfg1b *pmud, *pmudr;
  pmud=cfg1 + sizeof(struct cfg1a);
  pmudr=cfg1r + sizeof(struct cfg1a);
  
  printf("%d\n", cfg1);
  printf("%d\n",sizeof(struct cfg1a));
  printf("%d\n", pmud);
  
  
  char* chnams;
  char* chnamsr;
  
  uint16_t rate;
  
  int i = 0;

  // for every pmu
  for (i;i<pmua->num_pmu;i++)
    {
      if(read = getline(&line, &len, fp) != -1)
	{
	  pmud->id_code=(uint16_t)atoi(line); 
	  pmudr->id_code=atoi(line);
	  swap((char*)&(pmudr->id_code),sizeof(uint16_t));
	}
      
      if(read = getline(&line, &len, fp) != -1)
	{
	  pmud->format=(uint16_t)atoi(line); 
	  pmudr->format=(uint16_t)atoi(line); 
	  swap((char*)&(pmudr->format),sizeof(uint16_t));
	}
  
      if(read = getline(&line, &len, fp) != -1)
	{
	  pmud->phnmr=(uint16_t)atoi(line); 
	  pmudr->phnmr=(uint16_t)atoi(line); 
	  swap((char*)&(pmudr->phnmr),sizeof(uint16_t));
	
	}
      if(read = getline(&line, &len, fp) != -1)
	{
	  pmud->annmr=(uint16_t)atoi(line); 
	  pmudr->annmr=(uint16_t)atoi(line); 
	  swap((char*)&(pmudr->annmr),sizeof(uint16_t));
	
	}
      if(read = getline(&line, &len, fp) != -1)
	{
	  pmud->dgnmr=(uint16_t)atoi(line); 
	  pmudr->dgnmr=(uint16_t)atoi(line); 
	  swap((char*)&(pmudr->dgnmr),sizeof(uint16_t));
	}

      chnams = (char*)pmud + sizeof(struct cfg1b);
      chnamsr = (char*)pmudr + sizeof(struct cfg1b);
      				   
      //chnams
      int j =0;
      int num_chnams = (pmud->phnmr)+(pmud->annmr)+16*(pmud->dgnmr);
      for(j;j<num_chnams;j++)
	{
	  if(read = getline(&line, &len, fp) != -1)
	    {
	      strncpy (chnams,line,16); 
	      strncpy (chnamsr,line,16);
	      //swap((char*)chnamsr,16);
	    }
	  chnams+=16;
	  chnamsr+=16;
	}
      // conversion factors
      chnams = (uint32_t*)chnams;
      chnamsr = (uint32_t*)chnamsr;
      for (j=0;j<(pmud->phnmr + pmud->annmr + pmud->dgnmr);j++)
	{
	  if(read = getline(&line, &len, fp) != -1)
	    {
	      *chnams = (uint32_t)atoi(line); 
	      *chnamsr = (uint32_t)atoi(line);
	      swap((char*)chnamsr,sizeof(uint32_t));
	    }
	  chnams+=sizeof(uint32_t); //inc pointer
	  chnamsr+=sizeof(uint32_t); //inc pointer
	  
	}

      //fnom

      chnams = (uint16_t*)chnams;
      chnamsr = (uint16_t*)chnamsr;
      if(read = getline(&line, &len, fp) != -1)
	{
	  *chnams = (uint16_t)atoi(line); 
	  *chnamsr = (uint16_t)atoi(line);
	  swap((char*)chnamsr,sizeof(uint16_t));
	}
           
      chnams+=sizeof(uint16_t);
      chnamsr+=sizeof(uint16_t);


      //configuration change count

      if(read = getline(&line, &len, fp) != -1)
	{
	  *chnams = (uint16_t)atoi(line); 
	  *chnamsr = (uint16_t)atoi(line);
	  swap((char*)chnamsr,sizeof(uint16_t));
	}
           
      chnams+=sizeof(uint16_t);
      chnamsr+=sizeof(uint16_t);

      
      
      //if seg fault check the following uint16 - > cfg1b struct types?

      pmud = chnams;
      pmudr = chnams;
    }

  //data rate
  if(read = getline(&line, &len, fp) != -1)
    {
      *chnams = (uint16_t)atoi(line); 
      *chnamsr = (uint16_t)atoi(line);
      rate = (uint16_t)atoi(line);
      swap((char*)chnamsr,sizeof(uint16_t));
    }
  chnams+=sizeof(uint16_t);
  chnamsr+=sizeof(uint16_t);
  
  //call fill in last fields
  //frame size - ()
  uint16_t size;
  size = (chnams-(char*)cfg1+2);//2 for chk
  printf("framesize: %d\n\n",size);
  pmua->framesize=size;
  pmuar->framesize=size;
  swap((char*)&(pmuar->framesize),sizeof(pmuar->framesize));

  //frac of sec
  //soc
  //crc
  struct timespec tms;
  if(clock_gettime(CLOCK_REALTIME,&tms))
    {
      return -1;
    }
  pmua->soc = (uint32_t) tms.tv_sec;
  pmua->fracsec_a[0] = (uint16_t) (tms.tv_nsec*rate); //yeah yeah I cheated

  pmuar->soc = (uint32_t) tms.tv_sec;
  pmuar->fracsec_a[0] = (uint16_t) (tms.tv_nsec*rate); //yeah yeah I cheated

  swap((char*)&(pmuar->soc),sizeof(uint32_t));
  swap((char*)&(pmuar->fracsec_a),sizeof(uint16_t)); //doyuble check this?

  //calc crc
  uint16_t crc,crcr;

  crc=ComputeCRC(cfg1,size-2);
  crcr=ComputeCRC(cfg1r,size-2);

  printf("%04x\n",crcr);

  printf("%d\n",chnamsr);
  
  char* pa = (char*)&crcr;
  printf("%d\n",chnamsr);
    
  //add crc
  //*chnams = (uint16_t)crc; 
  //*chnamsr = crcr;
  //swap((char*)chnamsr,sizeof(uint16_t));
  
  chnamsr = (char*)chnamsr;
  printf("%d\n",chnamsr);
  
  *chnamsr =pa[1];
  printf("\n%04x\n\n",pa[0]);
  chnamsr++;
  *chnamsr =pa[0];
  printf("%d\n",chnamsr);
  
  //printf("%04x\n",(char*)chnamsr);
  
  
  //  printf("%d\n",chnams);
  chnams+=sizeof(uint16_t);
  chnamsr+=sizeof(uint16_t);
  
  //printf("%d\n",chnams);
  
  //send udp message
  struct  sockaddr_in si_other;
  int s, slen=sizeof(si_other);

  if ((s=socket(AF_INET,SOCK_DGRAM,IPPROTO_UDP))==-1)
    printf("socket");


  memset((char *) &si_other, 0, sizeof(si_other));
  si_other.sin_family = AF_INET;
  si_other.sin_port = htons(PORT);
  if (inet_aton(SRV_IP, &si_other.sin_addr)==0) {
    fprintf(stderr, "inet_aton() failed\n");
    exit(1);
  }

  for (i=0; i<NPACK; i++) {
    printf("Sending packet %d\n", i);
    //printf(cfg1r, "This is packet %d\n", i);
    if (sendto(s, cfg1r, (size), 0, &si_other, slen)==-1)
      printf("sendto()");
  }
  
  close(s);
  // end
  
  fclose(fp);
  if (line)
    free(line);
  //exit(EXIT_SUCCESS);  
  
  free(cfg1);
  free(cfg1r);

}




void cfg2()
{
  
  void *cfg1 = malloc(65535); // get largest size
  void *cfg1r = malloc(65535); //reversed big endian
  // read from file and fill in p
  FILE * fp;
  char * line = NULL;
  size_t len = 0;
  ssize_t read;

  fp = fopen("./config/cfg", "r");
  if (fp == NULL)
    exit(EXIT_FAILURE);

  struct cfg1a *pmua,*pmuar;
  pmua = cfg1;
  pmuar = cfg1r;
  // read first part of conf frame

  printf("starting reading");
  
  if(read = getline(&line, &len, fp) != -1)
    {
      pmua->sync=(uint16_t)atoi(line); 
      pmuar->sync=(uint16_t)43569;
      swap((char*)&(pmuar->sync),sizeof(uint16_t)); 
    }
  if(read = getline(&line, &len, fp) != -1)
    {
      pmua->id=(uint16_t)atoi(line); 
      pmuar->id=(uint16_t)atoi(line);
      swap((char*)&(pmuar->id),sizeof(uint16_t)); 
    }
  
  if(read = getline(&line, &len, fp) != -1)
    {
      pmua->fracsec_b=(uint8_t)atoi(line);
      pmuar->fracsec_b=(uint8_t)atoi(line);
      swap((char*)&(pmuar->fracsec_b),sizeof(uint8_t));
    }

  if(read = getline(&line, &len, fp) != -1)
    {
      pmua->time_base=(uint32_t)atoi(line); 
      pmuar->time_base=(uint32_t)atoi(line);
      swap((char*)&(pmuar->time_base),sizeof(uint32_t));
    }

  if(read = getline(&line, &len, fp) != -1)
    {
      pmua->num_pmu=(uint16_t)atoi(line); 
      pmuar->num_pmu=(uint16_t)atoi(line);
      swap((char*)&(pmuar->num_pmu),sizeof(uint16_t)); 
    }
  if(read = getline(&line, &len, fp) != -1)
    {
      strncpy ( pmua->stn,line,16); 
      strncpy (pmuar->stn,line,16);
      //swap((char*)&(pmuar->stn),16);
    }

  
  //middle loop part
  //printf("size of cfg1b %d\n", sizeof(struct cfg1b));

  struct cfg1b *pmud, *pmudr;
  pmud=cfg1 + sizeof(struct cfg1a);
  pmudr=cfg1r + sizeof(struct cfg1a);
  
  //p//rintf("%d\n", cfg1);
  //printf("%d\n",sizeof(struct cfg1a));
  //printf("%d\n", pmud);
  
  
  char* chnams;
  char* chnamsr;
  
  uint16_t rate;
  
  int i = 0;

  // for every pmu
  for (i;i<pmua->num_pmu;i++)
    {
      if(read = getline(&line, &len, fp) != -1)
	{
	  pmud->id_code=(uint16_t)atoi(line); 
	  pmudr->id_code=atoi(line);
	  swap((char*)&(pmudr->id_code),sizeof(uint16_t));
	}
      
      if(read = getline(&line, &len, fp) != -1)
	{
	  pmud->format=(uint16_t)atoi(line); 
	  pmudr->format=(uint16_t)atoi(line); 
	  swap((char*)&(pmudr->format),sizeof(uint16_t));
	}
  
      if(read = getline(&line, &len, fp) != -1)
	{
	  pmud->phnmr=(uint16_t)atoi(line); 
	  pmudr->phnmr=(uint16_t)atoi(line); 
	  swap((char*)&(pmudr->phnmr),sizeof(uint16_t));
	
	}
      if(read = getline(&line, &len, fp) != -1)
	{
	  pmud->annmr=(uint16_t)atoi(line); 
	  pmudr->annmr=(uint16_t)atoi(line); 
	  swap((char*)&(pmudr->annmr),sizeof(uint16_t));
	
	}
      if(read = getline(&line, &len, fp) != -1)
	{
	  pmud->dgnmr=(uint16_t)atoi(line); 
	  pmudr->dgnmr=(uint16_t)atoi(line); 
	  swap((char*)&(pmudr->dgnmr),sizeof(uint16_t));
	}

      chnams = (char*)pmud + sizeof(struct cfg1b);
      chnamsr = (char*)pmudr + sizeof(struct cfg1b);
      				   
      //chnams
      int j =0;
      int num_chnams = (pmud->phnmr)+(pmud->annmr)+16*(pmud->dgnmr);
      for(j;j<num_chnams;j++)
	{
	  if(read = getline(&line, &len, fp) != -1)
	    {
	      strncpy (chnams,line,16); 
	      strncpy (chnamsr,line,16);
	      //swap((char*)chnamsr,16);
	    }
	  chnams+=16;
	  chnamsr+=16;
	}
      // conversion factors
      chnams = (uint32_t*)chnams;
      chnamsr = (uint32_t*)chnamsr;
      for (j=0;j<(pmud->phnmr + pmud->annmr + pmud->dgnmr);j++)
	{
	  if(read = getline(&line, &len, fp) != -1)
	    {
	      *chnams = (uint32_t)atoi(line); 
	      *chnamsr = (uint32_t)atoi(line);
	      swap((char*)chnamsr,sizeof(uint32_t));
	    }
	  chnams+=sizeof(uint32_t); //inc pointer
	  chnamsr+=sizeof(uint32_t); //inc pointer
	  
	}

      //fnom

      chnams = (uint16_t*)chnams;
      chnamsr = (uint16_t*)chnamsr;
      if(read = getline(&line, &len, fp) != -1)
	{
	  *chnams = (uint16_t)atoi(line); 
	  *chnamsr = (uint16_t)atoi(line);
	  swap((char*)chnamsr,sizeof(uint16_t));
	}
           
      chnams+=sizeof(uint16_t);
      chnamsr+=sizeof(uint16_t);


      //configuration change count

      if(read = getline(&line, &len, fp) != -1)
	{
	  *chnams = (uint16_t)atoi(line); 
	  *chnamsr = (uint16_t)atoi(line);
	  swap((char*)chnamsr,sizeof(uint16_t));
	}
           
      chnams+=sizeof(uint16_t);
      chnamsr+=sizeof(uint16_t);

      
      
      //if seg fault check the following uint16 - > cfg1b struct types?

      pmud = chnams;
      pmudr = chnams;
    }

  //data rate
  if(read = getline(&line, &len, fp) != -1)
    {
      *chnams = (uint16_t)atoi(line); 
      *chnamsr = (uint16_t)atoi(line);
      rate = (int16_t)atoi(line);
      swap((char*)chnamsr,sizeof(uint16_t));
    }
  chnams+=sizeof(uint16_t);
  chnamsr+=sizeof(uint16_t);
  
  //call fill in last fields
  //frame size - ()
  uint16_t size;
  size = (chnams-(char*)cfg1+2);//2 for chk
  printf("framesize: %d\n\n",size);
  pmua->framesize=size;
  pmuar->framesize=size;
  swap((char*)&(pmuar->framesize),sizeof(pmuar->framesize));

  //frac of sec
  //soc
  //crc
  struct timespec tms;
  if(clock_gettime(CLOCK_REALTIME,&tms))
    {
      return -1;
    }
  pmua->soc = (uint32_t) tms.tv_sec;
  pmua->fracsec_a[0] = (uint16_t) (tms.tv_nsec*rate); //yeah yeah I cheated

  pmuar->soc = (uint32_t) tms.tv_sec;
  pmuar->fracsec_a[0] = (uint16_t) (tms.tv_nsec*rate); //yeah yeah I cheated

  swap((char*)&(pmuar->soc),sizeof(uint32_t));
  swap((char*)&(pmuar->fracsec_a),sizeof(uint16_t)); //doyuble check this?

  //calc crc
  uint16_t crc,crcr;

  crc=ComputeCRC(cfg1,size-2);
  crcr=ComputeCRC(cfg1r,size-2);

  printf("%04x\n",crcr);

  printf("%d\n",chnamsr);
  
  char* pa = (char*)&crcr;
  printf("%d\n",chnamsr);
    
  //add crc
  //*chnams = (uint16_t)crc; 
  //*chnamsr = crcr;
  //swap((char*)chnamsr,sizeof(uint16_t));
  
  chnamsr = (char*)chnamsr;
  printf("%d\n",chnamsr);
  
  *chnamsr =pa[1];
  printf("\n%04x\n\n",pa[0]);
  chnamsr++;
  *chnamsr =pa[0];
  printf("%d\n",chnamsr);
  
  //printf("%04x\n",(char*)chnamsr);
  
  
  //  printf("%d\n",chnams);
  chnams+=sizeof(uint16_t);
  chnamsr+=sizeof(uint16_t);
  
  //printf("%d\n",chnams);
  
  //send udp message
  struct  sockaddr_in si_other;
  int s, slen=sizeof(si_other);

  if ((s=socket(AF_INET,SOCK_DGRAM,IPPROTO_UDP))==-1)
    printf("socket");


  memset((char *) &si_other, 0, sizeof(si_other));
  si_other.sin_family = AF_INET;
  si_other.sin_port = htons(PORT);
  if (inet_aton(SRV_IP, &si_other.sin_addr)==0) {
    fprintf(stderr, "inet_aton() failed\n");
    exit(1);
  }

  for (i=0; i<NPACK; i++) {
    printf("Sending packet %d\n", i);
    //printf(cfg1r, "This is packet %d\n", i);
    if (sendto(s, cfg1r, (size), 0, &si_other, slen)==-1)
      printf("sendto()");
  }
  
  close(s);
  // end
  
  fclose(fp);
  if (line)
    free(line);
  //exit(EXIT_SUCCESS);  
  free(cfg1);
  free(cfg1r);
}




void data(int numPMU, uint16_t id,int rate, int *phnmr, int *annmr,int *dgnmr,  uint16_t *stat, uint32_t *phasors, uint16_t *freq, uint16_t *dfreq, uint32_t *analog, uint16_t *digital)
{
  //------------calc size
  int size;
  int i,j;
  struct timespec tms;
    
  size = 2 + 2 + 2 + 4 + 4; //header
  
  for (i=0;i<numPMU;i++){
    size += (2 + 4*phnmr[i] + 2 + 2 + 4 *annmr[i] +2 * dgnmr[i]); 
  } //data per pmu
  
  size += 2; //chk
  
  printf("size of data packet is %d",size);

  //----------------allocate size

  void *dataframe = malloc(size);

  void *dfptr;

  dfptr = dataframe;  
  
  //-------------fill memory in network format

  
  *(uint16_t*)dfptr = 43521; //aa01 data frame sync bit (1)
  swap((char*)dfptr,sizeof(uint16_t));
  dfptr += sizeof(uint16_t);

  *(uint16_t*)dfptr = size; // frame size(2)
  swap((char*)dfptr,sizeof(uint16_t));
  dfptr += sizeof(uint16_t);

  *(uint16_t*)dfptr = id; // id stream (idcode)(3)
  swap((char*)dfptr,sizeof(uint16_t));
  dfptr += sizeof(uint16_t);

  //time
  if(clock_gettime(CLOCK_REALTIME,&tms))
    {
      return -1;
    }
  
  *(uint32_t*)dfptr = (uint32_t) tms.tv_sec; //soc(4)
  swap((char*)dfptr,sizeof(uint32_t));
  dfptr += sizeof(uint32_t);
    
  *(uint32_t*)dfptr = (uint32_t) (tms.tv_nsec*rate);//(5) //yeah yeah I cheated
  swap((char*)dfptr,sizeof(uint32_t));
  dfptr += sizeof(uint32_t);
  
  
  // here starts our loop for number of phasors //6-11
  for (i=0;i<numPMU;i++){
    *(uint16_t*)dfptr = stat[i]; // stat per pmu (6)
    swap((char*)dfptr,sizeof(uint16_t));
    dfptr += sizeof(uint16_t);

    //inner loop phr
    for (j=0;j<phnmr[i];j++) {
      *(uint32_t*)dfptr = phasors[j];
      swap((char*)dfptr,sizeof(uint32_t));
      dfptr += sizeof(uint32_t);
    }

    *(uint16_t*)dfptr = freq[i]; // stat per pmu (6)
    swap((char*)dfptr,sizeof(uint16_t));
    dfptr += sizeof(uint16_t);

    *(uint16_t*)dfptr = dfreq[i]; // stat per pmu (6)
    swap((char*)dfptr,sizeof(uint16_t));
    dfptr += sizeof(uint16_t);

    //inner loop ann
    for (j=0;j<annmr[i];j++) {
      *(uint32_t*)dfptr = analog[j];
      swap((char*)dfptr,sizeof(uint32_t));
      dfptr += sizeof(uint32_t);
    }

    //inner loop dig
    for (j=0;j<dgnmr[i];j++) {
      *(uint16_t*)dfptr = digital[j];
      swap((char*)dfptr,sizeof(uint16_t));
      dfptr += sizeof(uint16_t);
    }
      
  } //end field 11
  
  //--------------compute chk
  uint16_t crc;
  crc = ComputeCRC(dataframe,size-2);
  char* cr = (char*)&crc;

  printf("\n%04x\n",crc);
  *(char*)dfptr = cr[1];
  dfptr++;
  *(char*)dfptr = cr[0];
  
  
  //---------------send udp


  //send udp message
  struct  sockaddr_in si_other;
  int s, slen=sizeof(si_other);
  
  if ((s=socket(AF_INET,SOCK_DGRAM,IPPROTO_UDP))==-1)
    printf("socket");
  
  
  memset((char *) &si_other, 0, sizeof(si_other));
  si_other.sin_family = AF_INET;
  si_other.sin_port = htons(PORT);
  if (inet_aton(SRV_IP, &si_other.sin_addr)==0) {
    fprintf(stderr, "inet_aton() failed\n");
    exit(1);
  }
  
  for (i=0; i<NPACK; i++) {
    printf("Sending packet %d\n", i);
    //printf(cfg1r, "This is packet %d\n", i);
    if (sendto(s, dataframe, (size), 0, &si_other, slen)==-1)
      printf("sendto()");
  }
  close(s);
  // end
  free(dataframe);
}




void test()
{

  int numPMU = 1;
  uint16_t id = 1000;
  int rate = 30;
  int* phnmr[numPMU];
  *phnmr = 4;
  int* annmr[numPMU];
  *annmr = 3;
  int* dgnmr[numPMU];
  *dgnmr = 0;

  uint16_t* stat[numPMU];
  *stat = 0;
  uint16_t* phasors[4];
  phasors[0] = 959119360;
  phasors[1] = 3815427708;
  phasors[2] = 3815387523;
  phasors[3] = 71565312;
  uint16_t* freq[numPMU];
  *freq = 59;
  uint16_t* dfreq[numPMU];
  *dfreq = 0;
  uint32_t* analog[3];
  analog[0] = 100;
  analog[1] = 1000;
  analog[2] = 10000;
  uint16_t* digital[numPMU];
  *digital = 0;
  
  
  //cfg1();
  int j=0;
  
  usleep(30000); // how to get 30 frames per second?

  while (j<10){
    cfg2();
    int i;
    i=0;
    
    while(i<30){
      usleep(33333);
      *freq = 50*i*i;
      phasors[0]+=10;
      phasors[1]+=5;
      phasors[2]-=5;
      phasors[3]++;
      data(numPMU, id, rate, phnmr, annmr, dgnmr, stat,
	   phasors,freq, dfreq, analog, digital);
      i++;
    }
  }
}

void main()
{
  test();
}
