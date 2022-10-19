\ Copyright (c) 2022 Travis Bemann
\
\ Permission is hereby granted, free of charge, to any person obtaining a copy
\ of this software and associated documentation files (the "Software"), to deal
\ in the Software without restriction, including without limitation the rights
\ to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
\ copies of the Software, and to permit persons to whom the Software is
\ furnished to do so, subject to the following conditions:
\ 
\ The above copyright notice and this permission notice shall be included in
\ all copies or substantial portions of the Software.
\ 
\ THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
\ IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
\ FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
\ AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
\ LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
\ OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
\ SOFTWARE.

compile-to-flash

begin-module include-fat32

  internal import
  oo import
  fat32 import
  
  \ Include filesystem not set exception
  : x-include-fs-not-set ( -- ) ." include filesystem not set" cr ;
  
  \ Include stack overflow exception
  : x-include-stack-overflow ( -- ) ." include stack overflow" cr ;
  
  begin-module include-fat32-internal
  
    \ Maximum include depth
    8 constant max-include-depth
    
    \ Include input buffer size
    256 constant include-buffer-size
    
    \ Include frame
    begin-structure include-frame-size
      
      \ Frame FAT32 file
      <fat32-file> class-size +field frame-file

      \ Evaluation index
      field: frame-eval-index

      \ Evaluation count
      field: frame-eval-count

      \ End of file condition
      field: frame-eof
      
      \ Frame offset
      field: frame-offset
    
      \ Saved evaluation buffer index pointer
      field: saved-eval-index-ptr

      \ Saved evaluation buffer count pointer
      field: saved-eval-count-ptr

      \ Saved evaluation buffer pointer
      field: saved-eval-ptr

      \ Saved evaluation refill word
      field: saved-eval-refill

      \ Saved evaluation EOF word
      field: saved-eval-eof

    end-structure
    
    \ The filesystem to include from
    variable include-fs
    
    \ Frame stack depth
    variable frame-depth
    
    \ Frame stack
    max-include-depth include-frame-size * buffer: include-stack
    
    \ Include input buffer content length
    variable include-buffer-content-len
    
    \ Include input buffer
    include-buffer-size buffer: include-buffer
    
    \ Get the top frame of the include stack
    : include-stack-top@ ( -- frame )
      include-stack frame-depth @ 1- include-frame-size * +
    ;
    
    \ Get the next include stack frame
    : include-stack-next@ ( -- frame )
      include-stack frame-depth @ include-frame-size * +
    ;

    \ Save evaluation state
    : save-eval ( -- )
      eval-index-ptr @ include-stack-top@ saved-eval-index-ptr !
      eval-count-ptr @ include-stack-top@ saved-eval-count-ptr !
      eval-ptr @ include-stack-top@ saved-eval-ptr !
      eval-refill @ include-stack-top@ saved-eval-refill !
      eval-eof @ include-stack-top@ saved-eval-eof !
    ;

    \ Restore evaluation state
    : restore-eval ( -- )
      include-stack-top@ saved-eval-index-ptr @ eval-index-ptr !
      include-stack-top@ saved-eval-count-ptr @ eval-count-ptr !
      include-stack-top@ saved-eval-ptr @ eval-ptr !
      include-stack-top@ saved-eval-refill @ eval-refill !
      include-stack-top@ saved-eval-eof @ eval-eof !
    ;
    
    \ Read code from a file
    : read-file-into-buffer ( -- )
      include-buffer include-buffer-content-len @ +
      include-buffer-size include-buffer-content-len @ -
      include-stack-top@ frame-file read-file
      include-buffer-content-len +!
    ;
    
    \ Get the executable line length
    : execute-line-len ( -- bytes )
      include-buffer-content-len @ 0 ?do
        include-buffer i + c@ dup $0A = swap $0D = or if
          i 1+ unloop exit
        then
      loop
      include-buffer-content-len @
    ;
    
    \ Update frame state
    : update-frame ( -- )
      0 include-stack-top@ frame-eval-index !
      execute-line-len dup include-stack-top@ frame-offset @ +
      include-stack-top@ frame-file file-size@ =
      include-stack-top@ frame-eof !
      dup dup 0> if
        1- include-buffer + c@ dup $0A = swap $0D = or if 1- then
      then
      include-stack-top@ frame-eval-count !
    ;

    \ Refill file
    : frame-eval-refill ( -- )
      execute-line-len dup include-stack-top@ frame-offset +!
      dup negate include-buffer-content-len +!
      include-buffer + include-buffer include-buffer-content-len @ move
      read-file-into-buffer
      update-frame
    ;
    
    \ Check end of file condition
    : frame-eval-eof ( -- eof? ) include-stack-top@ frame-eof @ ;
      
    \ Set up interpreting from file
    : setup-interpret ( -- )
      include-stack-top@ frame-eval-index eval-index-ptr !
      include-stack-top@ frame-eval-count eval-count-ptr !
      include-buffer eval-ptr !
      ['] frame-eval-refill eval-refill !
      ['] frame-eval-eof eval-eof !
    ;
    
    \ Un-nest an include
    : unnest-include ( -- )
      frame-depth @ 1- 0 max frame-depth !
      frame-depth @ 0> if
        include-stack-top@ frame-offset @ seek-set
        include-stack-top@ frame-file seek-file
        0 include-buffer-content-len !
        read-file-into-buffer
      then 
    ;
    
    \ Execute an included file
    : execute-file ( -- )
      save-eval 1 prompt-disabled +!
      [:
        read-file-into-buffer
        include-buffer-content-len @ 0> if
          update-frame
          setup-interpret 
          outer
        then
      ;] try
      -1 prompt-disabled +!
      restore-eval
      unnest-include
      ?raise
    ;
    
    \ Initialize FAT32 including
    : init-include-fat32 ( -- )
      0 include-fs !
      0 include-buffer-content-len !
      0 frame-depth !
    ;
  
  end-module> import
  
  \ Set the include filesystem
  : include-fs! ( fs -- ) include-fs ! ;
  
  \ Get the include filesystem
  : include-fs@ ( fs -- ) include-fs @ ;
  
  \ Load a file
  : load-file ( file -- )
    include-fs @ averts x-include-fs-not-set
    frame-depth @ max-include-depth < averts x-include-stack-overflow
    include-stack-next@ frame-file <fat32-file> class-size move
    include-stack-next@ frame-file tell-file include-stack-next@ frame-offset !
    1 frame-depth +!
    0 include-buffer-content-len !
    execute-file
  ;
  
  \ Include a file
  : included ( c-addr u -- )
    include-fs @ averts x-include-fs-not-set
    frame-depth @ max-include-depth < averts x-include-stack-overflow
    [:
      include-stack-next@ frame-file swap open-file
    ;] include-fs@ with-root-path
    0 include-stack-next@ frame-offset !
    1 frame-depth +!
    0 include-buffer-content-len !
    execute-file
  ;
  
  \ Include a file
  : include ( "path" -- )
    token dup 0<> averts x-token-expected included
  ;

end-module> import

: init ( -- )
  init
  [ include-fat32 :: include-fat32-internal ] :: init-include-fat32
;

reboot