import React, { ReactNode } from 'react';
import { Navbar, NavbarBrand, Spinner } from 'reactstrap';
import { Link } from 'react-router-dom';
import { useIsMutating, useQueryClient } from '@tanstack/react-query';

const NavBar = ({ children }: { children?: ReactNode }) => {
    const isMutating = useIsMutating();
    
    return (
        <header>
            <Navbar className="border-bottom box-shadow mb-3" container light>
                <div className="d-flex align-items-start w-100">
                    <h3 className="koti-title">
                        <Link to="/">
                            <span className="koti-logo-wrapper">
                                <img width="24" height="24" alt="KoTi" src="/koti-logo.svg" />
                                <div className={'spinner ' + (isMutating ? '' : 'hide')}><Spinner type="grow" /></div>
                            </span><span>KoTi</span></Link></h3>
                    {children && <><h3>&nbsp;&rsaquo;&nbsp;</h3>{children}</>}
                </div>
            </Navbar>
        </header>
    );
};

export default NavBar;
