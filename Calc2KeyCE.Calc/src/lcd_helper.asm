assume  adl=1

section .text

public  _lcd_Configure
public  _lcd_Reset

macro spi cmd, params&
    ld	a, cmd
    call	spiCmd
    match any, params
	    iterate param, any
		    ld	a, param
		    call	spiParam
	    end iterate
    end match
end macro

_lcd_Configure:
                            ; Memory Address Control https://wikiti.brandonw.net/index.php?title=84PCE:Ports:D000	
                            ; Bit 2: Horizontal Refresh Order
                            ; Bit 3: BGR Order
                            ; Bit 4: Vertical Refresh Order
                            ; Bit 5: Column Major Update
                            ; Bit 6: Column Update Order
                            ; Bit 7: Row Update Order
    spi    $36,$6c          
    spi    $2a,0,0,0,$ef    ; Column Set XS = 0 XE = 239 
    spi    $2b,0,0,1,$3f    ; Row Set YS = 0 YE = 319 

    ret

_lcd_Reset:
    spi    $36,$08
    spi    $2a,0,0,1,$3f
    spi    $2b,0,0,0,$ef

    ret

; Input: A = parameter
spiParam:
    scf ; First bit is set for data
    virtual
		jr	nc, $
		load .jr_nc : byte from $$
	end virtual
	db	.jr_nc

; Input: A = command
spiCmd:
    or a,a ; First bit is clear for commands
    ld hl,0F80818h
    call spiWrite
    ld l,h
    ld (hl),001h
spiWait:
    ld l,00Dh
spiWait1:
    ld a,(hl)
    and a,0F0h
    jr nz,spiWait1
    dec l
spiWait2:
    bit 2,(hl)
    jr nz,spiWait2
    ld l,h
    ld (hl),a
    ret
spiWrite:
    ld b,3
spiWriteLoop:
    rla
    rla
    rla
    ld (hl),a ; send 3 bits
    djnz spiWriteLoop
    ret